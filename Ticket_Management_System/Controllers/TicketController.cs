using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using TicketDAL.Dal;
using TicketModel;
using TicketModel.ViewModels;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Controllers
{
    [Authorize(Roles = "Employee")]
    public class TicketController : Controller
    {
        private TicketDataAccess ticketDAL = new TicketDataAccess();

        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private int GetCurrentUserId()
        {
            var principal = (ClaimsPrincipal)User;
            var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null || string.IsNullOrEmpty(claim.Value))
                throw new UnauthorizedAccessException("Unable to identify current user.");
            return Convert.ToInt32(claim.Value);
        }

        [HttpGet]
        public ActionResult Index(string search, DateTime? date, string sortOrder, int? statusFilter)
        {
            try
            {
                int uid = GetCurrentUserId();
                var tickets = ticketDAL.GetTicketsByUserId(uid, search, statusFilter, null);

                if (date.HasValue)
                    tickets = tickets.Where(t => t.CreatedOn.Date == date.Value.Date).ToList();

                tickets = sortOrder == "oldest"
                    ? tickets.OrderBy(t => t.CreatedOn).ToList()
                    : tickets.OrderByDescending(t => t.CreatedOn).ToList();

                ViewBag.Search = search;
                ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.SortOrder = sortOrder ?? "newest";
                ViewBag.StatusFilter = statusFilter;
                return View(tickets);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "Index");
                return View(new List<TicketModel.Models.TicketModel>());
            }
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateTicketViewModel model, List<HttpPostedFileBase> Attachments)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                int uid = GetCurrentUserId();
                int newTicketId = ticketDAL.CreateTicket(model.Title, model.Description, uid);

                SaveAttachments(Attachments, newTicketId, uid);

                TempData["SuccessMessage"] = "Complaint raised successfully! Ticket ID: TICK-" + newTicketId.ToString("D4");

                var notifyDAL = new NotificationDataAccess();
                foreach (var adminId in notifyDAL.GetAllAdminUserIds())
                    notifyDAL.Insert(adminId, "New complaint raised: \"" + model.Title + "\"", newTicketId);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "Create");
                ModelState.AddModelError("", "Failed to create ticket: " + ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            try
            {
                if (id <= 0) return HttpNotFound("Invalid ticket ID.");

                int uid = GetCurrentUserId();
                var ticket = ticketDAL.GetTicketById(id, uid);
                if (ticket == null) return HttpNotFound("Ticket not found or access denied.");

                ViewBag.Comments = ticketDAL.GetCommentsByTicketId(id);
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                return View(ticket);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "Details");
                TempData["ErrorMessage"] = "An error occurred loading the ticket.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("Index");
                }
                int uid = GetCurrentUserId();
                var ticket = ticketDAL.GetTicketById(id, uid);

                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "This ticket doesn't exist or you don't have permission to edit it.";
                    return RedirectToAction("Index");
                }
                if (ticket.StatusId != 1)
                {
                    TempData["ErrorMessage"] = "This ticket can no longer be edited since it's already being processed.";
                    return RedirectToAction("Details", new { id });
                }

                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                var model = new EditTicketViewModel
                {
                    TicketId = ticket.TicketId,
                    Title = ticket.Title,
                    Description = ticket.Description
                };
                return View(model);
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "Edit");
                TempData["ErrorMessage"] = "An error occurred loading the ticket.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EditTicketViewModel model, List<HttpPostedFileBase> Attachments)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                    return View(model);
                }

                int uid = GetCurrentUserId();

                if (Attachments != null)
                {
                    var actualFiles = Attachments.Where(f => f != null && f.ContentLength > 0).ToList();
                    var existingCount = ticketDAL.GetAttachmentsByTicketId(model.TicketId) != null
                        ? ticketDAL.GetAttachmentsByTicketId(model.TicketId).Count : 0;

                    if (existingCount + actualFiles.Count > 5)
                    {
                        ModelState.AddModelError("", "You can only have 5 attachments per ticket. You already have " + existingCount + ", so you can add at most " + (5 - existingCount) + " more.");
                        ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                        return View(model);
                    }

                    foreach (var file in actualFiles)
                    {
                        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (Array.IndexOf(AllowedExtensions, ext) < 0)
                        {
                            ModelState.AddModelError("", "'" + file.FileName + "' has an invalid file type. Allowed: jpg, jpeg, png, gif, bmp, pdf, txt, doc, docx.");
                            ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                            return View(model);
                        }
                        if (file.ContentLength > MaxFileSizeBytes)
                        {
                            ModelState.AddModelError("", "'" + file.FileName + "' exceeds the 5 MB size limit.");
                            ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                            return View(model);
                        }
                    }
                }

                bool updated = ticketDAL.UpdateTicket(model.TicketId, uid, model.Title, model.Description);
                if (updated)
                {
                    SaveAttachments(Attachments, model.TicketId, uid);
                    TempData["SuccessMessage"] = "Ticket updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Unable to update ticket.";
                }
                return RedirectToAction("Details", new { id = model.TicketId });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "Edit");
                TempData["ErrorMessage"] = "An error occurred updating the ticket.";
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(model.TicketId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int attachmentId, int ticketId)
        {
            try
            {
                if (attachmentId <= 0 || ticketId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid request.";
                    return RedirectToAction("Index");
                }
                ticketDAL.DeleteAttachment(attachmentId, GetCurrentUserId());
                TempData["SuccessMessage"] = "Attachment removed.";
                return RedirectToAction("Edit", new { id = ticketId });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "DeleteAttachment");
                TempData["ErrorMessage"] = "An error occurred removing the attachment.";
                return RedirectToAction("Edit", new { id = ticketId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("Index");
                }
                bool deleted = ticketDAL.DeleteTicket(id, GetCurrentUserId());
                TempData["SuccessMessage"] = deleted
                    ? "Ticket deleted successfully."
                    : "Unable to delete ticket. It may already be in progress.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "Delete");
                TempData["ErrorMessage"] = "An error occurred deleting the ticket.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAttachment(int ticketId, HttpPostedFileBase file)
        {
            try
            {
                if (ticketId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("Index");
                }
                if (file != null && file.ContentLength > 0)
                {
                    SaveAttachments(new List<HttpPostedFileBase> { file }, ticketId, GetCurrentUserId());
                    TempData["SuccessMessage"] = "Attachment uploaded successfully.";
                }
                return RedirectToAction("Details", new { id = ticketId });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "AddAttachment");
                TempData["ErrorMessage"] = "An error occurred uploading the attachment.";
                return RedirectToAction("Details", new { id = ticketId });
            }
        }

        private void SaveAttachments(List<HttpPostedFileBase> files, int ticketId, int userId)
        {
            try
            {
                if (files == null) return;

                string uploadFolder = Server.MapPath("~/Uploads/Tickets/" + ticketId);
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                foreach (var file in files)
                {
                    if (file == null || file.ContentLength == 0) continue;

                    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (Array.IndexOf(AllowedExtensions, ext) < 0)
                    {
                        ModelState.AddModelError("", "File type " + ext + " is not allowed.");
                        continue;
                    }
                    if (file.ContentLength > MaxFileSizeBytes)
                    {
                        ModelState.AddModelError("", file.FileName + " exceeds 5 MB limit.");
                        continue;
                    }

                    string safeFileName = Guid.NewGuid() + ext;
                    string fullPath = Path.Combine(uploadFolder, safeFileName);
                    file.SaveAs(fullPath);

                    string relativePath = "/Uploads/Tickets/" + ticketId + "/" + safeFileName;
                    ticketDAL.AddAttachment(ticketId, relativePath, userId);
                }
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "SaveAttachments");
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
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
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

                var ticket = ticketDAL.GetTicketById(ticketId, uid);
                if (ticket != null)
                    Ticket_Management_System.Helpers.CommentNotifier.NotifyStakeholders(
                        ticketId, ticket.Title, uid, "Employee");

                TempData["SuccessMessage"] = "Message sent.";
                return RedirectToAction("Details", new { id = ticketId });
            }
            catch (Exception ex)
            {
                Helpers.Logger.LogToFile(ex, "Ticket", "AddComment");
                TempData["ErrorMessage"] = "An error occurred sending the message.";
                return RedirectToAction("Details", new { id = ticketId });
            }
        }
    }
}