using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using TicketDAL.Dal;
using TicketModel.ViewModels;
using Ticket_Management_System.Helpers;
using System.Text;

namespace Ticket_Management_System.Controllers
{
    [Authorize(Roles = "Support Executive")]
    public class SupportController : Controller
    {
        private SupportDataAccess supportDAL = new SupportDataAccess();
        private TicketDataAccess ticketDAL = new TicketDataAccess();

        private int GetCurrentUserId()
        {
            var principal = (ClaimsPrincipal)User;
            var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null) throw new UnauthorizedAccessException("Unable to identify current user.");
            return Convert.ToInt32(claim.Value);
        }

        private void LogException(Exception ex, string actionName)
        {
            try
            {
                Helpers.Logger.LogToFile(ex, "Support", actionName);
                new TicketDAL.Dal.ErrorLogDataAccess().LogError(
                    controllerName: "Support",
                    actionName: actionName,
                    exceptionMessage: ex.Message,
                    stackTrace: ex.StackTrace,
                    userEmail: (User != null && User.Identity != null && User.Identity.IsAuthenticated) ? User.Identity.Name : "Anonymous",
                    requestUrl: Request != null && Request.Url != null ? Request.Url.ToString() : null);
            }
            catch { }
        }

        public ActionResult Index()
        {
            try
            {
                int uid = GetCurrentUserId();
                ViewBag.NeedsPriorityCount = supportDAL.GetNeedsPriority(uid).Count;
                ViewBag.BoardCount = supportDAL.GetAssignedTickets(uid, null, null, null).Count;
                ViewBag.ArchiveCount = supportDAL.GetCompletedArchive(uid).Count;
                return View();
            }
            catch (Exception ex)
            {
                LogException(ex, "Index");
                return View();
            }
        }

        // ── UPDATED: added search, date, sortOrder parameters ──────────────────
        public ActionResult NeedsPriority(string search, DateTime? date, string sortOrder)
        {
            try
            {
                int uid = GetCurrentUserId();
                var tickets = supportDAL.GetNeedsPriority(uid);

                if (!string.IsNullOrWhiteSpace(search))
                    tickets = tickets.Where(t =>
                        t.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (t.RaisedByName != null && t.RaisedByName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        t.TicketId.ToString().Contains(search)
                    ).ToList();

                if (date.HasValue)
                    tickets = tickets.Where(t => t.CreatedOn.Date == date.Value.Date).ToList();

                tickets = sortOrder == "oldest"
                    ? tickets.OrderBy(t => t.CreatedOn).ToList()
                    : tickets.OrderByDescending(t => t.CreatedOn).ToList();

                ViewBag.Search = search;
                ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.SortOrder = sortOrder ?? "newest";
                return View(tickets);
            }
            catch (Exception ex)
            {
                LogException(ex, "NeedsPriority");
                return View(new List<TicketModel.Models.TicketModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetPriority(int ticketId, int priorityId)
        {
            try
            {
                if (ticketId <= 0 || priorityId < 1 || priorityId > 3)
                {
                    TempData["ErrorMessage"] = "Invalid ticket or priority value.";
                    return RedirectToAction("NeedsPriority");
                }
                supportDAL.SetPriority(ticketId, GetCurrentUserId(), priorityId);
                TempData["SuccessMessage"] = "Priority set. Ticket moved to your board.";
                return RedirectToAction("NeedsPriority");
            }
            catch (Exception ex)
            {
                LogException(ex, "SetPriority");
                TempData["ErrorMessage"] = "An error occurred setting priority.";
                return RedirectToAction("NeedsPriority");
            }
        }

        public ActionResult Board(string search, int? priorityId)
        {
            try
            {
                var tickets = supportDAL.GetAssignedTickets(GetCurrentUserId(), search, null, priorityId);
                ViewBag.Search = search;
                ViewBag.PriorityId = priorityId;
                return View(tickets);
            }
            catch (Exception ex)
            {
                LogException(ex, "Board");
                return View(new List<TicketModel.Models.TicketModel>());
            }
        }

        [HttpPost]
        public JsonResult UpdateStatusAjax(int ticketId, int statusId)
        {
            try
            {
                if (ticketId <= 0 || statusId < 1 || statusId > 4)
                    return Json(new { success = false, message = "Invalid ticket or status value." });

                int uid = GetCurrentUserId();
                bool success = supportDAL.UpdateStatusOnly(ticketId, uid, statusId);

                if (success && statusId == 4)
                {
                    var ticket = supportDAL.GetTicketByIdForSupport(ticketId, uid);
                    var notifyDAL = new NotificationDataAccess();
                    notifyDAL.Insert(ticket.RaisedbyUserId,
                        "Your issue \"" + ticket.Title + "\" has been resolved. Please review.", ticketId);
                    foreach (var adminId in notifyDAL.GetAllAdminUserIds())
                        notifyDAL.Insert(adminId,
                            "Ticket \"" + ticket.Title + "\" marked Completed by Support.", ticketId);
                }

                return Json(new { success });
            }
            catch (Exception ex)
            {
                LogException(ex, "UpdateStatusAjax");
                return Json(new { success = false, message = "An error occurred updating status." });
            }
        }

        // ── UPDATED: added search, date, sortOrder, priorityFilter parameters ──
        public ActionResult CompletedArchive(string search, DateTime? date, string sortOrder, int? priorityFilter)
        {
            try
            {
                int uid = GetCurrentUserId();
                var tickets = supportDAL.GetCompletedArchive(uid);

                if (!string.IsNullOrWhiteSpace(search))
                    tickets = tickets.Where(t =>
                        t.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (t.RaisedByName != null && t.RaisedByName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        t.TicketId.ToString().Contains(search)
                    ).ToList();

                if (date.HasValue)
                    tickets = tickets.Where(t =>
                        t.TicketClosedDate.HasValue &&
                        t.TicketClosedDate.Value.Date == date.Value.Date
                    ).ToList();

                if (priorityFilter.HasValue)
                    tickets = tickets.Where(t => t.PriorityId == priorityFilter.Value).ToList();

                tickets = sortOrder == "oldest"
                    ? tickets.OrderBy(t => t.TicketClosedDate).ToList()
                    : tickets.OrderByDescending(t => t.TicketClosedDate).ToList();

                ViewBag.Search = search;
                ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.SortOrder = sortOrder ?? "newest";
                ViewBag.PriorityFilter = priorityFilter;
                return View(tickets);
            }
            catch (Exception ex)
            {
                LogException(ex, "CompletedArchive");
                return View(new List<TicketModel.Models.TicketModel>());
            }
        }

        public ActionResult ExportCompletedArchiveCsv(string search, DateTime? date, string sortOrder, int? priorityFilter)
        {
            try
            {
                int uid = GetCurrentUserId();
                var tickets = supportDAL.GetCompletedArchive(uid);

                if (!string.IsNullOrWhiteSpace(search))
                    tickets = tickets.Where(t =>
                        t.Title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (t.RaisedByName != null && t.RaisedByName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        t.TicketId.ToString().Contains(search)
                    ).ToList();

                if (date.HasValue)
                    tickets = tickets.Where(t =>
                        t.TicketClosedDate.HasValue &&
                        t.TicketClosedDate.Value.Date == date.Value.Date
                    ).ToList();

                if (priorityFilter.HasValue)
                    tickets = tickets.Where(t => t.PriorityId == priorityFilter.Value).ToList();

                tickets = sortOrder == "oldest"
                    ? tickets.OrderBy(t => t.TicketClosedDate).ToList()
                    : tickets.OrderByDescending(t => t.TicketClosedDate).ToList();

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(",", new[]
                {
            "Ticket ID", "Title", "Priority", "Raised By", "Completed On"
        }.Select(CsvEscape)));

                foreach (var t in tickets)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                "TICK-" + t.TicketId.ToString("D4"),
                t.Title,
                string.IsNullOrEmpty(t.PriorityName) ? "Normal" : t.PriorityName,
                t.RaisedByName,
                t.TicketClosedDate.HasValue ? t.TicketClosedDate.Value.ToString("yyyy-MM-dd HH:mm") : ""
            }.Select(CsvEscape)));
                }

                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                string fileName = "CompletedArchive_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                LogException(ex, "ExportCompletedArchiveCsv");
                TempData["ErrorMessage"] = "An error occurred exporting the archive.";
                return RedirectToAction("CompletedArchive");
            }
        }

        private static string CsvEscape(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            bool needsQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
            string escaped = field.Replace("\"", "\"\"");
            return needsQuoting ? "\"" + escaped + "\"" : escaped;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MoveBackToProgress(int ticketId)
        {
            try
            {
                if (ticketId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("CompletedArchive");
                }
                int uid = GetCurrentUserId();
                supportDAL.MoveBackToProgress(ticketId, uid);

                var ticket = supportDAL.GetTicketByIdForSupport(ticketId, uid);
                new NotificationDataAccess().Insert(ticket.RaisedbyUserId,
                    "Your issue \"" + ticket.Title + "\" has been reopened for further review.", ticketId);

                TempData["SuccessMessage"] = "Ticket moved back to active board.";
                return RedirectToAction("CompletedArchive");
            }
            catch (Exception ex)
            {
                LogException(ex, "MoveBackToProgress");
                TempData["ErrorMessage"] = "An error occurred moving the ticket.";
                return RedirectToAction("CompletedArchive");
            }
        }

        public ActionResult Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("Index");
                }
                int uid = GetCurrentUserId();
                var ticket = supportDAL.GetTicketByIdForSupport(id, uid);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found or not assigned to you.";
                    return RedirectToAction("Index");
                }
                ViewBag.Comments = ticketDAL.GetCommentsByTicketId(id);
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                return View(ticket);
            }
            catch (Exception ex)
            {
                LogException(ex, "Details");
                TempData["ErrorMessage"] = "An error occurred loading the ticket.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddComment(int ticketId, string commentText, HttpPostedFileBase chatFile)
        {
            try
            {
                if (ticketId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("Index");
                }

                int uid = GetCurrentUserId();
                string finalComment = commentText ?? "";

                if (chatFile != null && chatFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(chatFile.FileName);
                    string fileExt = Path.GetExtension(fileName).ToLower();
                    string newFileName = Guid.NewGuid().ToString() + "_" + fileName;
                    string uploadFolder = Server.MapPath("~/Uploads/Chat/" + ticketId);
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);
                    chatFile.SaveAs(Path.Combine(uploadFolder, newFileName));

                    string relativePath = "/Uploads/Chat/" + ticketId + "/" + newFileName;
                    bool isImg = fileExt == ".jpg" || fileExt == ".jpeg" || fileExt == ".png"
                                          || fileExt == ".gif" || fileExt == ".bmp";
                    string tag = isImg
                        ? "[IMAGE:" + relativePath + "]"
                        : "[FILE:" + relativePath + "|" + fileName + "]";
                    finalComment = string.IsNullOrWhiteSpace(finalComment)
                        ? tag : finalComment + " " + tag;
                }

                if (string.IsNullOrWhiteSpace(finalComment))
                {
                    TempData["ErrorMessage"] = "Message or attachment is required.";
                    return RedirectToAction("Details", new { id = ticketId });
                }

                ticketDAL.AddComment(ticketId, uid, finalComment);

                var ticket = supportDAL.GetTicketByIdForSupport(ticketId, uid);
                if (ticket != null)
                    Ticket_Management_System.Helpers.CommentNotifier.NotifyStakeholders(
                        ticketId, ticket.Title, uid, "Support");

                TempData["SuccessMessage"] = "Message sent.";
                return RedirectToAction("Details", new { id = ticketId });
            }
            catch (Exception ex)
            {
                LogException(ex, "AddComment");
                TempData["ErrorMessage"] = "An error occurred sending the message.";
                return RedirectToAction("Details", new { id = ticketId });
            }
        }
       
        public ActionResult CreateTicket()
        {
            try
            {
                ViewBag.ActiveTab = "SupportCreateTicket";
                return View();
            }
            catch (Exception ex)
            {
                LogException(ex, "CreateTicket");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTicket(string title, string description, List<HttpPostedFileBase> Attachments)
        {
            try
            {
                ViewBag.ActiveTab = "SupportCreateTicket";

                if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 3 || title.Trim().Length > 100)
                {
                    TempData["ErrorMessage"] = "Title must be between 3 and 100 characters.";
                    return View();
                }
                if (string.IsNullOrWhiteSpace(description) || description.Trim().Length < 10 || description.Trim().Length > 2000)
                {
                    TempData["ErrorMessage"] = "Description must be between 10 and 2000 characters.";
                    return View();
                }

                if (Attachments != null)
                {
                    var actualFiles = Attachments.Where(f => f != null && f.ContentLength > 0).ToList();
                    if (actualFiles.Count > 5)
                    {
                        TempData["ErrorMessage"] = "You can upload a maximum of 5 files at a time.";
                        return View();
                    }
                    string[] allowedTypes = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
                    foreach (var file in actualFiles)
                    {
                        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (Array.IndexOf(allowedTypes, ext) < 0)
                        {
                            TempData["ErrorMessage"] = "'" + file.FileName + "' has an invalid file type. Allowed: jpg, jpeg, png, gif, bmp, pdf, txt, doc, docx.";
                            return View();
                        }
                        if (file.ContentLength > 5 * 1024 * 1024)
                        {
                            TempData["ErrorMessage"] = "'" + file.FileName + "' exceeds the 5 MB size limit.";
                            return View();
                        }
                    }
                }

                int uid = GetCurrentUserId();
                int newTicketId = ticketDAL.CreateTicket(title.Trim(), description.Trim(), uid);

                SaveMyAttachments(Attachments, newTicketId, uid);

                var notifyDAL = new NotificationDataAccess();
                foreach (var adminId in notifyDAL.GetAllAdminUserIds())
                    notifyDAL.Insert(adminId, "New complaint raised: \"" + title.Trim() + "\"", newTicketId);

                TempData["SuccessMessage"] = "Complaint raised successfully! Ticket ID: TICK-" + newTicketId.ToString("D4");
                return RedirectToAction("MyComplaints");
            }
            catch (Exception ex)
            {
                LogException(ex, "CreateTicket");
                TempData["ErrorMessage"] = "An error occurred creating the complaint.";
                return View();
            }
        }

        public ActionResult MyComplaints(string search, DateTime? date, string sortOrder)
        {
            try
            {
                int uid = GetCurrentUserId();
                var tickets = ticketDAL.GetTicketsByUserId(uid, search, null, null);

                if (date.HasValue)
                    tickets = tickets.Where(t => t.CreatedOn.Date == date.Value.Date).ToList();

                tickets = sortOrder == "oldest"
                    ? tickets.OrderBy(t => t.CreatedOn).ToList()
                    : tickets.OrderByDescending(t => t.CreatedOn).ToList();

                ViewBag.Search = search;
                ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.SortOrder = sortOrder ?? "newest";
                ViewBag.ActiveTab = "SupportMyComplaints";
                return View(tickets);
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaints");
                TempData["ErrorMessage"] = "An error occurred loading your complaints.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult MyComplaintDetails(int id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("MyComplaints");
                int uid = GetCurrentUserId();
                var ticket = ticketDAL.GetTicketById(id, uid);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Complaint not found.";
                    return RedirectToAction("MyComplaints");
                }
                ViewBag.Comments = ticketDAL.GetCommentsByTicketId(id);
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                ViewBag.ActiveTab = "SupportMyComplaints";
                return View(ticket);
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaintDetails");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }

        public ActionResult MyComplaintEdit(int id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("MyComplaints");
                int uid = GetCurrentUserId();
                var ticket = ticketDAL.GetTicketById(id, uid);
                if (ticket == null || ticket.StatusId != 1)
                {
                    TempData["ErrorMessage"] = "Complaint not found or cannot be edited.";
                    return RedirectToAction("MyComplaints");
                }
                var model = new EditTicketViewModel
                {
                    TicketId = ticket.TicketId,
                    Title = ticket.Title,
                    Description = ticket.Description
                };
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                ViewBag.ActiveTab = "SupportMyComplaints";
                return View(model);
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaintEdit");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MyComplaintEdit(EditTicketViewModel model, List<HttpPostedFileBase> Attachments)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                    ViewBag.ActiveTab = "SupportMyComplaints";
                    return View(model);
                }

                if (Attachments != null)
                {
                    var actualFiles = Attachments.Where(f => f != null && f.ContentLength > 0).ToList();
                    var existingCount = ticketDAL.GetAttachmentsByTicketId(model.TicketId).Count;
                    if (existingCount + actualFiles.Count > 5)
                    {
                        ModelState.AddModelError("", "You can only have 5 attachments per ticket. You already have " + existingCount + ", so you can add at most " + (5 - existingCount) + " more.");
                        ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                        ViewBag.ActiveTab = "SupportMyComplaints";
                        return View(model);
                    }
                    string[] allowedTypes = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
                    foreach (var file in actualFiles)
                    {
                        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (Array.IndexOf(allowedTypes, ext) < 0)
                        {
                            ModelState.AddModelError("", "'" + file.FileName + "' has an invalid file type.");
                            ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                            ViewBag.ActiveTab = "SupportMyComplaints";
                            return View(model);
                        }
                        if (file.ContentLength > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "'" + file.FileName + "' exceeds the 5 MB size limit.");
                            ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                            ViewBag.ActiveTab = "SupportMyComplaints";
                            return View(model);
                        }
                    }
                }

                int uid = GetCurrentUserId();
                ticketDAL.UpdateTicket(model.TicketId, uid, model.Title, model.Description);
                SaveMyAttachments(Attachments, model.TicketId, uid);
                TempData["SuccessMessage"] = "Complaint updated successfully.";
                return RedirectToAction("MyComplaints");
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaintEdit");
                TempData["ErrorMessage"] = "An error occurred.";
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                ViewBag.ActiveTab = "SupportMyComplaints";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMyComplaint(int id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("MyComplaints");
                ticketDAL.DeleteTicket(id, GetCurrentUserId());
                TempData["SuccessMessage"] = "Complaint deleted successfully.";
                return RedirectToAction("MyComplaints");
            }
            catch (Exception ex)
            {
                LogException(ex, "DeleteMyComplaint");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MyComplaintAddComment(int ticketId, string commentText, HttpPostedFileBase chatFile)
        {
            try
            {
                int uid = GetCurrentUserId();
                string finalComment = commentText ?? "";

                if (chatFile != null && chatFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(chatFile.FileName);
                    string fileExt = Path.GetExtension(fileName).ToLower();
                    string newFileName = Guid.NewGuid().ToString() + "_" + fileName;
                    string uploadFolder = Server.MapPath("~/Uploads/Chat/" + ticketId);
                    if (!Directory.Exists(uploadFolder))
                        Directory.CreateDirectory(uploadFolder);
                    chatFile.SaveAs(Path.Combine(uploadFolder, newFileName));

                    string relativePath = "/Uploads/Chat/" + ticketId + "/" + newFileName;
                    bool isImg = fileExt == ".jpg" || fileExt == ".jpeg" || fileExt == ".png"
                                          || fileExt == ".gif" || fileExt == ".bmp";
                    string tag = isImg
                        ? "[IMAGE:" + relativePath + "]"
                        : "[FILE:" + relativePath + "|" + fileName + "]";
                    finalComment = string.IsNullOrWhiteSpace(finalComment)
                        ? tag : finalComment + " " + tag;
                }

                if (string.IsNullOrWhiteSpace(finalComment))
                {
                    TempData["ErrorMessage"] = "Message or attachment is required.";
                    return RedirectToAction("MyComplaintDetails", new { id = ticketId });
                }

                ticketDAL.AddComment(ticketId, uid, finalComment);

                var ticket = ticketDAL.GetTicketById(ticketId, uid);
                if (ticket != null)
                    Ticket_Management_System.Helpers.CommentNotifier.NotifyStakeholders(
                        ticketId, ticket.Title, uid, "Support");

                TempData["SuccessMessage"] = "Message sent.";
                return RedirectToAction("MyComplaintDetails", new { id = ticketId });
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaintAddComment");
                TempData["ErrorMessage"] = "An error occurred sending the message.";
                return RedirectToAction("MyComplaintDetails", new { id = ticketId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MyComplaintDeleteAttachment(int attachmentId, int ticketId)
        {
            try
            {
                ticketDAL.DeleteAttachment(attachmentId, GetCurrentUserId());
                TempData["SuccessMessage"] = "Attachment removed.";
                return RedirectToAction("MyComplaintEdit", new { id = ticketId });
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaintDeleteAttachment");
                TempData["ErrorMessage"] = "An error occurred removing the attachment.";
                return RedirectToAction("MyComplaintEdit", new { id = ticketId });
            }
        }

        private void SaveMyAttachments(List<HttpPostedFileBase> files, int ticketId, int userId)
        {
            try
            {
                if (files == null) return;
                string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
                string uploadFolder = Server.MapPath("~/Uploads/Tickets/" + ticketId);
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                foreach (var file in files)
                {
                    if (file == null || file.ContentLength == 0) continue;
                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (Array.IndexOf(allowed, ext) < 0) continue;
                    if (file.ContentLength > 5 * 1024 * 1024) continue;

                    string safeFileName = Guid.NewGuid() + ext;
                    file.SaveAs(Path.Combine(uploadFolder, safeFileName));
                    ticketDAL.AddAttachment(ticketId, "/Uploads/Tickets/" + ticketId + "/" + safeFileName, userId);
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "SaveMyAttachments");
            }
        }
    }
}