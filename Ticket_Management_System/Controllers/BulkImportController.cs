using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Security.Claims;
using System.Web.Mvc;
using ClosedXML.Excel;
using TicketDAL.Dal;
using TicketModel.ViewModels;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Controllers
{
    [Authorize(Roles = "Administrator,Support Executive,Employee")]
    public class BulkImportController : Controller
    {
        private TicketDataAccess ticketDAL = new TicketDataAccess();
        private AdminDataAccess adminDAL = new AdminDataAccess();

        private const int MaxRowsPerImport = 200;

        private int GetCurrentUserId()
        {
            var principal = (ClaimsPrincipal)User;
            var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null || string.IsNullOrEmpty(claim.Value))
                throw new UnauthorizedAccessException("Unable to identify current user.");
            return Convert.ToInt32(claim.Value);
        }

        public ActionResult Index()
        {
            ViewBag.ActiveTab = "BulkImport";
            return View();
        }

        public ActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("Complaints");
                sheet.Cell(1, 1).Value = "Title";
                sheet.Cell(1, 2).Value = "Description";

                sheet.Cell(2, 1).Value = "Example: Printer not working";
                sheet.Cell(2, 2).Value = "The office printer on 3rd floor is jammed and won't print.";

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "BulkComplaintTemplate.xlsx");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase bulkFile)
        {
            var result = new BulkImportResultViewModel();

            try
            {
                if (bulkFile == null || bulkFile.ContentLength == 0)
                {
                    TempData["ErrorMessage"] = "Please choose an Excel file to upload.";
                    return RedirectToAction("Index");
                }

                string ext = Path.GetExtension(bulkFile.FileName).ToLowerInvariant();
                if (ext != ".xlsx")
                {
                    TempData["ErrorMessage"] = "Only .xlsx (Excel) files are supported.";
                    return RedirectToAction("Index");
                }

                List<BulkComplaintRow> rows;
                try
                {
                    rows = ParseExcel(bulkFile);
                }
                catch (Exception parseEx)
                {
                    Helpers.Logger.LogToFile(parseEx, "BulkImport", "ParseFile");
                    TempData["ErrorMessage"] = "Could not read the file. Please make sure it matches the template format.";
                    return RedirectToAction("Index");
                }

                if (rows.Count == 0)
                {
                    TempData["ErrorMessage"] = "No data rows found in the file.";
                    return RedirectToAction("Index");
                }
                if (rows.Count > MaxRowsPerImport)
                {
                    TempData["ErrorMessage"] = "A single import is limited to " + MaxRowsPerImport + " rows. Please split your file into smaller batches.";
                    return RedirectToAction("Index");
                }

                int uid = GetCurrentUserId();
                bool isAdmin = User.IsInRole("Administrator");

                var notifyDAL = new NotificationDataAccess();
                List<int> adminIdsToNotify = isAdmin ? null : notifyDAL.GetAllAdminUserIds();

                int rowNumber = 1; // row 1 is the header
                foreach (var row in rows)
                {
                    rowNumber++;
                    var rowResult = new BulkImportRowResult { RowNumber = rowNumber, Title = row.Title };

                    try
                    {
                        // ── Same validation rules as the existing single-ticket create flows ──
                        if (string.IsNullOrWhiteSpace(row.Title) || row.Title.Trim().Length < 3 || row.Title.Trim().Length > 100)
                        {
                            rowResult.ErrorMessage = "Title must be between 3 and 100 characters.";
                            result.Results.Add(rowResult);
                            continue;
                        }
                        if (string.IsNullOrWhiteSpace(row.Description) || row.Description.Trim().Length < 10 || row.Description.Trim().Length > 2000)
                        {
                            rowResult.ErrorMessage = "Description must be between 10 and 2000 characters.";
                            result.Results.Add(rowResult);
                            continue;
                        }

                        // ── Create the ticket — SQL Server's IDENTITY column handles the next ID
                        // automatically and safely (no manual max+1 needed, and no race condition
                        // even if two people import at the same moment). New tickets always start
                        // unassigned (AssignedtoUserId stays NULL), so they land directly in the
                        // Admin's "Unassigned Tickets" queue — same as any single-created ticket.
                        int newTicketId = isAdmin
                            ? adminDAL.CreateTicket(row.Title.Trim(), row.Description.Trim(), uid, uid)
                            : ticketDAL.CreateTicket(row.Title.Trim(), row.Description.Trim(), uid);

                        // ── Notifications, matching each role's existing single-create behavior ──
                        if (isAdmin)
                        {
                            notifyDAL.Insert(uid, "You raised a new complaint: \"" + row.Title.Trim() + "\"", newTicketId);
                        }
                        else
                        {
                            foreach (var adminId in adminIdsToNotify)
                                notifyDAL.Insert(adminId, "New complaint raised: \"" + row.Title.Trim() + "\"", newTicketId);
                        }

                        rowResult.Success = true;
                        rowResult.TicketId = newTicketId;
                        result.Results.Add(rowResult);
                    }
                    catch (Exception rowEx)
                    {
                        Helpers.Logger.LogToFile(rowEx, "BulkImport", "ProcessRow");
                        rowResult.ErrorMessage = "Unexpected error creating this ticket.";
                        result.Results.Add(rowResult);
                    }
                }

                TempData["SuccessMessage"] = result.SuccessCount > 0
                       ? result.SuccessCount + " complaint(s) created successfully."
                           + (result.FailureCount > 0 ? " " + result.FailureCount + " row(s) failed — see details below." : "")
                       : "No complaints were created — see the errors below.";

                return View("Results", result);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "BulkImport", "Upload");
                TempData["ErrorMessage"] = "An error occurred processing the bulk upload.";
                return RedirectToAction("Index");
            }
        }

        private List<BulkComplaintRow> ParseExcel(HttpPostedFileBase file)
        {
            var rows = new List<BulkComplaintRow>();
            using (var workbook = new XLWorkbook(file.InputStream))
            {
                var sheet = workbook.Worksheet(1);
                var usedRows = sheet.RowsUsed().Skip(1); // skip header row

                foreach (var xlRow in usedRows)
                {
                    string title = xlRow.Cell(1).GetString();
                    string description = xlRow.Cell(2).GetString();

                    if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
                        continue; // skip fully blank rows

                    rows.Add(new BulkComplaintRow { Title = title, Description = description });
                }
            }
            return rows;
        }

        private class BulkComplaintRow
        {
            public string Title { get; set; }
            public string Description { get; set; }
        }
    }
}