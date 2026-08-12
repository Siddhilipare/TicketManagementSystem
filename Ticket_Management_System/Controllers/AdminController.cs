using System;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Security.Claims;
using TicketDAL.Dal;
using TicketModel.ViewModels;
using System.Linq;
using Ticket_Management_System.Helpers.Security;
using Ticket_Management_System.Helpers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

namespace Ticket_Management_System.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private AdminDataAccess adminDAL = new AdminDataAccess();

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
                Helpers.Logger.LogToFile(ex, "Admin", actionName);
                new TicketDAL.Dal.ErrorLogDataAccess().LogError(
                    controllerName: "Admin",
                    actionName: actionName,
                    exceptionMessage: ex.Message,
                    stackTrace: ex.StackTrace,
                    userEmail: (User != null && User.Identity != null && User.Identity.IsAuthenticated) ? User.Identity.Name : "Anonymous",
                    requestUrl: Request != null && Request.Url != null ? Request.Url.ToString() : null);
            }
            catch { }
        }

        public ActionResult Dashboard()
        {
            try
            {
             
                var allTickets = adminDAL.GetAllTickets(null, null, null);
                var allUsers = adminDAL.GetAllUsers();

                DateTime weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

                ViewBag.TotalTickets = allTickets.Count;
                ViewBag.OpenTickets = allTickets.Count(t => t.StatusId == 1);
                ViewBag.InProgressTickets = allTickets.Count(t => t.StatusId == 2);
                ViewBag.CompletedTickets = allTickets.Count(t => t.StatusId == 4);
                ViewBag.ResolvedThisWeek = allTickets.Count(t =>
                   t.StatusId == 4 &&
                   t.TicketClosedDate.HasValue &&
                   t.TicketClosedDate.Value >= weekStart);
                ViewBag.UnassignedTickets = allTickets.Count(t => t.AssignedtoUserId == null);
                ViewBag.TotalUsers = allUsers.Count;
                ViewBag.ActiveSupport = allUsers.Count(u =>
                u.RoleName == "Support Executive" && u.IsActive);

              
                ViewBag.RecentTickets = allTickets
                    .OrderByDescending(t => t.CreatedOn)
                    .Take(10)
                    .ToList();

              
                var statusCounts = new[]
               {
                (int)ViewBag.OpenTickets,
                (int)ViewBag.InProgressTickets,
                (int)ViewBag.CompletedTickets
              };
                ViewBag.StatusChartLabelsJson = JsonConvert.SerializeObject(new[] { "To Do", "In Progress", "Completed" });
                ViewBag.StatusChartDataJson = JsonConvert.SerializeObject(statusCounts);
              
                var priorityCounts = new[]
                {
            allTickets.Count(t => t.PriorityId == 1),  
            allTickets.Count(t => t.PriorityId == 2),  
            allTickets.Count(t => t.PriorityId == 3), 
            allTickets.Count(t => t.PriorityId == null) 
        };
                ViewBag.PriorityChartLabelsJson = JsonConvert.SerializeObject(new[] { "Low", "Medium", "High", "Unset" });
                ViewBag.PriorityChartDataJson = JsonConvert.SerializeObject(priorityCounts);

              
                var trendDays = Enumerable.Range(0, 7)
                    .Select(offset => DateTime.Today.AddDays(-6 + offset))
                    .ToList();

                var trendLabels = trendDays.Select(d => d.ToString("MMM dd")).ToList();
                var createdPerDay = trendDays.Select(d => allTickets.Count(t => t.CreatedOn.Date == d)).ToList();
                var resolvedPerDay = trendDays.Select(d => allTickets.Count(t =>
                    t.TicketClosedDate.HasValue && t.TicketClosedDate.Value.Date == d)).ToList();

                ViewBag.TrendLabelsJson = JsonConvert.SerializeObject(trendLabels);
                ViewBag.TrendCreatedJson = JsonConvert.SerializeObject(createdPerDay);
                ViewBag.TrendResolvedJson = JsonConvert.SerializeObject(resolvedPerDay);

                ViewBag.ActiveTab = "AdminDashboard";
                return View();
            }
            catch (Exception ex)
            {
                LogException(ex, "Dashboard");
                TempData["ErrorMessage"] = "An error occurred loading the dashboard.";
                return RedirectToAction("AllTickets");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleUserActive(int userId, bool isActive, string returnUrl = null)
        {
            try
            {
                if (userId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid user.";
                    return RedirectToAction("ManageUsers");
                }
                adminDAL.ToggleUserActive(userId, isActive, GetCurrentUserId());
                TempData["SuccessMessage"] = isActive ? "User activated." : "User deactivated.";
                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                LogException(ex, "ToggleUserActive");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("ManageUsers");
            }
        }

        public ActionResult EditUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid user.";
                    return RedirectToAction("ManageUsers");
                }
                var users = adminDAL.GetAllUsers();
                var user = users.Find(u => u.UserId == id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("ManageUsers");
                }
                var model = new EditUserViewModel { UserId = user.UserId, UserName = user.UserName, RoleId = user.RoleId };
                return View(model);
            }
            catch (Exception ex)
            {
                LogException(ex, "EditUser");
                TempData["ErrorMessage"] = "An error occurred loading the user.";
                return RedirectToAction("ManageUsers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(EditUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                if (!Regex.IsMatch(model.UserName != null ? model.UserName.Trim() : "", @"^[a-zA-Z\s\-']+$"))
                {
                    ModelState.AddModelError("UserName", "Name can only contain letters, spaces, hyphens, or apostrophes.");
                    return View(model);
                }

                adminDAL.UpdateUser(model.UserId, model.UserName.Trim(), model.RoleId, GetCurrentUserId());
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                LogException(ex, "EditUser");
                TempData["ErrorMessage"] = "An error occurred updating the user.";
                return RedirectToAction("ManageUsers");
            }
        }

        public ActionResult ManageUsers(string search)
        {
            try
            {
                var users = adminDAL.GetStaffUsers();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string term = search.Trim();
                    users = users.Where(u =>
                        (!string.IsNullOrEmpty(u.UserName) && u.UserName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(u.Email) && u.Email.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }

                ViewBag.Search = search;
                return View(users);
            }
            catch (Exception ex)
            {
                LogException(ex, "ManageUsers");
                return View(new List<TicketModel.Models.UserListModel>());
            }
        }


        public ActionResult ManageEmployees(string search)
        {
            try
            {
                var employees = adminDAL.GetEmployees();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string term = search.Trim();
                    employees = employees.Where(e =>
                        (!string.IsNullOrEmpty(e.UserName) && e.UserName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(e.Email) && e.Email.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }

                ViewBag.Search = search;
                return View(employees);
            }
            catch (Exception ex)
            {
                LogException(ex, "ManageEmployees");
                return View(new List<TicketModel.Models.UserListModel>());
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddStaffUser(AddStaffUserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(" ", errors) });
                }

                if (!Regex.IsMatch(model.UserName != null ? model.UserName.Trim() : "", @"^[a-zA-Z\s\-']+$"))
                {
                    return Json(new { success = false, message = "Name can only contain letters, spaces, hyphens, or apostrophes." });
                }

                UserDAL userDAL = new UserDAL();
                var existing = userDAL.GetUserByEmail(model.Email);
                if (existing != null)
                {
                    return Json(new { success = false, message = "This email is already registered." });
                }

                var hashResult = PasswordHasher.HashPassword(model.Password);
                int newUserId = userDAL.InsertUser(model.Email, hashResult.Hash, hashResult.Salt, model.RoleId, isVerified: true);
                userDAL.InsertUserDetail(newUserId, model.UserName.Trim(), null, null, null, null);

                return Json(new { success = true, message = "User created successfully." });
            }
            catch (Exception ex)
            {
                LogException(ex, "AddStaffUser");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid user.";
                    return RedirectToAction("ManageUsers");
                }
                adminDAL.ToggleUserActive(userId, false, GetCurrentUserId());
                TempData["SuccessMessage"] = "User deactivated successfully.";
                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                LogException(ex, "DeleteUser");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("ManageUsers");
            }
        }

        public ActionResult AllTickets(string search, DateTime? date, int? assignedToUserId, string sortOrder, string view)
        {
            try
            {
                var allTickets = adminDAL.GetAllTickets(search, null, null);

                if (date.HasValue)
                    allTickets = allTickets.Where(t => t.CreatedOn.Date == date.Value.Date).ToList();

                if (assignedToUserId.HasValue)
                    allTickets = allTickets.Where(t => t.AssignedtoUserId == assignedToUserId.Value).ToList();

                allTickets = sortOrder == "oldest"
                    ? allTickets.OrderBy(t => t.CreatedOn).ToList()
                    : allTickets.OrderByDescending(t => t.CreatedOn).ToList();

                ViewBag.AssignedTickets = allTickets.Where(t => t.AssignedtoUserId != null).ToList();
                ViewBag.UnassignedTickets = allTickets.Where(t => t.AssignedtoUserId == null).ToList();
                ViewBag.SupportExecutives = adminDAL.GetSupportExecutives();
                ViewBag.Search = search;
                ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.AssignedToFilter = assignedToUserId;
                ViewBag.SortOrder = sortOrder ?? "newest";
                ViewBag.CurrentView = view ?? "unassigned";

                return View(allTickets);
            }
            catch (Exception ex)
            {
                LogException(ex, "AllTickets");
                TempData["ErrorMessage"] = "An error occurred loading tickets.";
                return View(new List<TicketModel.Models.TicketModel>());
            }
        }

        public ActionResult ManageTicket(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("AllTickets");
                }
                var ticket = adminDAL.GetTicketByIdForAdmin(id);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("AllTickets");
                }
                ViewBag.SupportExecutives = adminDAL.GetSupportExecutives();
                ViewBag.Attachments = new TicketDataAccess().GetAttachmentsByTicketId(id);
                ViewBag.Comments = new TicketDataAccess().GetCommentsByTicketId(id);
                return View(ticket);
            }
            catch (Exception ex)
            {
                LogException(ex, "ManageTicket");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("AllTickets");
            }
        }

        // Combined save: assigns/reassigns the support executive AND optionally
        // posts a comment in a single round-trip, so the admin never has to
        // navigate away just to add a comment after assigning.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageTicket(int ticketId, int assignedToUserId, string commentText)
        {
            try
            {
                if (ticketId <= 0 || assignedToUserId <= 0)
                {
                    TempData["ErrorMessage"] = "Please select a support executive before saving.";
                    return RedirectToAction("ManageTicket", new { id = ticketId });
                }

                var ticket = adminDAL.GetTicketByIdForAdmin(ticketId);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("AllTickets");
                }

                int adminId = GetCurrentUserId();
                var successParts = new List<string>();

                // ── Part 1: Assign / Reassign (only if the selection actually changed) ──
                bool isNewAssignment = !ticket.AssignedtoUserId.HasValue;
                bool isReassignment = ticket.AssignedtoUserId.HasValue && ticket.AssignedtoUserId.Value != assignedToUserId;

                if (isNewAssignment || isReassignment)
                {
                    int? previousAssigneeId = ticket.AssignedtoUserId;
                    adminDAL.AssignTicket(ticketId, assignedToUserId, adminId);

                    var notifyDAL = new NotificationDataAccess();
                    notifyDAL.Insert(assignedToUserId, "You have been assigned complaint: \"" + ticket.Title + "\"", ticketId);

                    string employeeMsg = isReassignment
                        ? "Your complaint \"" + ticket.Title + "\" has been reassigned to a new support executive."
                        : "Your complaint \"" + ticket.Title + "\" has been assigned to a support executive.";
                    notifyDAL.Insert(ticket.RaisedbyUserId, employeeMsg, ticketId);

                    if (isReassignment && previousAssigneeId.HasValue)
                    {
                        notifyDAL.Insert(previousAssigneeId.Value, "Complaint \"" + ticket.Title + "\" has been reassigned to another support executive.", ticketId);
                    }

                    // Email the newly assigned support executive
                    try
                    {
                        var assignedUser = new UserDAL().GetUserById(assignedToUserId);
                        if (assignedUser != null && !string.IsNullOrWhiteSpace(assignedUser.Email))
                        {
                            string subject = "New Complaint Assigned: TICK-" + ticketId.ToString("D4");
                            string body = "<p>Hello,</p>"
                                + "<p>The complaint <strong>\"" + ticket.Title + "\"</strong> (TICK-" + ticketId.ToString("D4") + ") has been assigned to you.</p>"
                                + "<p>Please log in to Simplify to review the details and respond.</p>";
                            EmailHelper.Send(assignedUser.Email, subject, body);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        LogException(emailEx, "ManageTicket_SendAssignmentEmail");
                    }

                    successParts.Add("Ticket assigned successfully.");
                }

                // ── Part 2: Comment (fully optional) ──
                if (!string.IsNullOrWhiteSpace(commentText))
                {
                    new TicketDataAccess().AddComment(ticketId, adminId, commentText.Trim());
                    successParts.Add("Comment sent successfully.");
                }

                TempData["SuccessMessage"] = successParts.Count > 0
                    ? string.Join(" ", successParts)
                    : "No changes were made.";

                return RedirectToAction("AllTickets");
            }
            catch (Exception ex)
            {
                LogException(ex, "ManageTicket");
                TempData["ErrorMessage"] = "An error occurred saving your changes.";
                return RedirectToAction("AllTickets");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTicket(EditTicketAdminViewModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);

                adminDAL.UpdateTicketAsAdmin(
                    model.TicketId, model.Title, model.Description,
                    model.PriorityId ?? 0, model.StatusId, GetCurrentUserId());
                TempData["SuccessMessage"] = "Ticket updated successfully.";
                return RedirectToAction("AllTickets");
            }
            catch (Exception ex)
            {
                LogException(ex, "EditTicket");
                TempData["ErrorMessage"] = "An error occurred updating the ticket.";
                return RedirectToAction("AllTickets");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTicket(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "Invalid ticket.";
                    return RedirectToAction("AllTickets");
                }
                adminDAL.DeleteTicketAsAdmin(id, GetCurrentUserId());
                TempData["SuccessMessage"] = "Ticket deleted successfully.";
                return RedirectToAction("AllTickets");
            }
            catch (Exception ex)
            {
                LogException(ex, "DeleteTicket");
                TempData["ErrorMessage"] = "An error occurred deleting the ticket.";
                return RedirectToAction("AllTickets");
            }
        }

        public ActionResult CreateTicket()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                LogException(ex, "CreateTicket");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("AllTickets");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTicket(string title, string description, List<System.Web.HttpPostedFileBase> Attachments)
        {
            try
            {
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

                // Validate attachments
                if (Attachments != null)
                {
                    var actualFiles = Attachments.Where(f => f != null && f.ContentLength > 0).ToList();
                    if (actualFiles.Count > 5)
                    {
                        TempData["ErrorMessage"] = "You can upload a maximum of 5 files at a time.";
                        return View();
                    }
                    foreach (var file in actualFiles)
                    {
                        string ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                        string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
                        if (Array.IndexOf(allowed, ext) < 0)
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

                int adminId = GetCurrentUserId();
                int newTicketId = adminDAL.CreateTicket(title.Trim(), description.Trim(), adminId, adminId);

                // Save attachments
                SaveAttachments(Attachments, newTicketId, adminId);

                // Notify admin himself
                var notifyDAL = new NotificationDataAccess();
                notifyDAL.Insert(adminId, "You raised a new complaint: \"" + title.Trim() + "\"", newTicketId);

                TempData["SuccessMessage"] = "Ticket created successfully! Ticket ID: TICK-" + newTicketId.ToString("D4");
                return RedirectToAction("AllTickets");
            }
            catch (Exception ex)
            {
                LogException(ex, "CreateTicket");
                TempData["ErrorMessage"] = "An error occurred creating the ticket.";
                return View();
            }
        }

        public ActionResult MyComplaints(string search, DateTime? date, string sortOrder)
        {
            try
            {
                int adminId = GetCurrentUserId();
                var tickets = new TicketDataAccess().GetTicketsByUserId(adminId, search, null, null);

                if (date.HasValue)
                    tickets = tickets.Where(t => t.CreatedOn.Date == date.Value.Date).ToList();

                tickets = sortOrder == "oldest"
                    ? tickets.OrderBy(t => t.CreatedOn).ToList()
                    : tickets.OrderByDescending(t => t.CreatedOn).ToList();

                ViewBag.Search = search;
                ViewBag.SelectedDate = date.HasValue ? date.Value.ToString("yyyy-MM-dd") : null;
                ViewBag.SortOrder = sortOrder ?? "newest";
                ViewBag.ActiveTab = "AdminMyComplaints";
                return View(tickets);
            }
            catch (Exception ex)
            {
                LogException(ex, "MyComplaints");
                TempData["ErrorMessage"] = "An error occurred loading your complaints.";
                return RedirectToAction("AllTickets");
            }
        }

        private void SaveAttachments(List<System.Web.HttpPostedFileBase> files, int ticketId, int userId)
        {
            try
            {
                if (files == null) return;
                string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
                string uploadFolder = System.Web.HttpContext.Current.Server.MapPath("~/Uploads/Tickets/" + ticketId);
                if (!System.IO.Directory.Exists(uploadFolder))
                    System.IO.Directory.CreateDirectory(uploadFolder);

                foreach (var file in files)
                {
                    if (file == null || file.ContentLength == 0) continue;
                    string ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (Array.IndexOf(allowed, ext) < 0) continue;
                    if (file.ContentLength > 5 * 1024 * 1024) continue;

                    string safeFileName = Guid.NewGuid() + ext;
                    file.SaveAs(System.IO.Path.Combine(uploadFolder, safeFileName));
                    new TicketDataAccess().AddAttachment(ticketId, "/Uploads/Tickets/" + ticketId + "/" + safeFileName, userId);
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "SaveAttachments");
            }
        }

        public ActionResult AdminTicketDetails(int id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("MyComplaints");
                int adminId = GetCurrentUserId();
                var ticketDAL = new TicketDataAccess();
                var ticket = ticketDAL.GetTicketById(id, adminId);
                if (ticket == null)
                {
                    TempData["ErrorMessage"] = "Ticket not found.";
                    return RedirectToAction("MyComplaints");
                }
                ViewBag.Comments = ticketDAL.GetCommentsByTicketId(id);
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                ViewBag.ActiveTab = "AdminMyComplaints";
                return View(ticket);
            }
            catch (Exception ex)
            {
                LogException(ex, "AdminTicketDetails");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }

        public ActionResult AdminTicketEdit(int id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("MyComplaints");
                int adminId = GetCurrentUserId();
                var ticketDAL = new TicketDataAccess();
                var ticket = ticketDAL.GetTicketById(id, adminId);
                if (ticket == null || ticket.StatusId != 1)
                {
                    TempData["ErrorMessage"] = "Ticket not found or cannot be edited.";
                    return RedirectToAction("MyComplaints");
                }
                var model = new TicketModel.ViewModels.EditTicketViewModel
                {
                    TicketId = ticket.TicketId,
                    Title = ticket.Title,
                    Description = ticket.Description
                };
                ViewBag.Attachments = ticketDAL.GetAttachmentsByTicketId(id);
                ViewBag.ActiveTab = "AdminMyComplaints";
                return View(model);
            }
            catch (Exception ex)
            {
                LogException(ex, "AdminTicketEdit");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminTicketEdit(TicketModel.ViewModels.EditTicketViewModel model, List<System.Web.HttpPostedFileBase> Attachments)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Attachments = new TicketDataAccess().GetAttachmentsByTicketId(model.TicketId);
                    ViewBag.ActiveTab = "AdminMyComplaints";
                    return View(model);
                }

                if (Attachments != null)
                {
                    var actualFiles = Attachments.Where(f => f != null && f.ContentLength > 0).ToList();
                    var existingCount = new TicketDataAccess().GetAttachmentsByTicketId(model.TicketId).Count;
                    if (existingCount + actualFiles.Count > 5)
                    {
                        ModelState.AddModelError("", "You can only have 5 attachments per ticket. You already have " + existingCount + ", so you can add at most " + (5 - existingCount) + " more.");
                        ViewBag.Attachments = new TicketDataAccess().GetAttachmentsByTicketId(model.TicketId);
                        ViewBag.ActiveTab = "AdminMyComplaints";
                        return View(model);
                    }
                    foreach (var file in actualFiles)
                    {
                        string ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
                        string[] allowed = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".doc", ".docx" };
                        if (Array.IndexOf(allowed, ext) < 0)
                        {
                            ModelState.AddModelError("", "'" + file.FileName + "' has an invalid file type.");
                            ViewBag.Attachments = new TicketDataAccess().GetAttachmentsByTicketId(model.TicketId);
                            ViewBag.ActiveTab = "AdminMyComplaints";
                            return View(model);
                        }
                        if (file.ContentLength > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "'" + file.FileName + "' exceeds the 5 MB size limit.");
                            ViewBag.Attachments = new TicketDataAccess().GetAttachmentsByTicketId(model.TicketId);
                            ViewBag.ActiveTab = "AdminMyComplaints";
                            return View(model);
                        }
                    }
                }

                int adminId = GetCurrentUserId();
                new TicketDataAccess().UpdateTicket(model.TicketId, adminId, model.Title, model.Description);
                SaveAttachments(Attachments, model.TicketId, adminId);
                TempData["SuccessMessage"] = "Ticket updated successfully.";
                return RedirectToAction("MyComplaints");
            }
            catch (Exception ex)
            {
                LogException(ex, "AdminTicketEdit");
                TempData["ErrorMessage"] = "An error occurred.";
                ViewBag.Attachments = new TicketDataAccess().GetAttachmentsByTicketId(model.TicketId);
                ViewBag.ActiveTab = "AdminMyComplaints";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminDeleteMyTicket(int id)
        {
            try
            {
                if (id <= 0) return RedirectToAction("MyComplaints");
                new TicketDataAccess().DeleteTicket(id, GetCurrentUserId());
                TempData["SuccessMessage"] = "Ticket deleted successfully.";
                return RedirectToAction("MyComplaints");
            }
            catch (Exception ex)
            {
                LogException(ex, "AdminDeleteMyTicket");
                TempData["ErrorMessage"] = "An error occurred.";
                return RedirectToAction("MyComplaints");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminAddComment(int ticketId, string commentText, System.Web.HttpPostedFileBase chatFile)
        {
            try
            {
                int uid = GetCurrentUserId();
                string finalComment = commentText ?? "";

                if (chatFile != null && chatFile.ContentLength > 0)
                {
                    string fileName = System.IO.Path.GetFileName(chatFile.FileName);
                    string fileExt = System.IO.Path.GetExtension(fileName).ToLower();
                    string newFileName = Guid.NewGuid().ToString() + "_" + fileName;
                    string uploadFolder = System.Web.HttpContext.Current.Server.MapPath("~/Uploads/Chat/" + ticketId);
                    if (!System.IO.Directory.Exists(uploadFolder))
                        System.IO.Directory.CreateDirectory(uploadFolder);
                    chatFile.SaveAs(System.IO.Path.Combine(uploadFolder, newFileName));
                    string relativePath = "/Uploads/Chat/" + ticketId + "/" + newFileName;
                    bool isImg = fileExt == ".jpg" || fileExt == ".jpeg" || fileExt == ".png" || fileExt == ".gif" || fileExt == ".bmp";
                    string tag = isImg ? "[IMAGE:" + relativePath + "]" : "[FILE:" + relativePath + "|" + fileName + "]";
                    finalComment = string.IsNullOrWhiteSpace(finalComment) ? tag : finalComment + " " + tag;
                }

                if (string.IsNullOrWhiteSpace(finalComment))
                {
                    TempData["ErrorMessage"] = "Message or attachment is required.";
                    return RedirectToAction("AdminTicketDetails", new { id = ticketId });
                }

                new TicketDataAccess().AddComment(ticketId, uid, finalComment);
                TempData["SuccessMessage"] = "Message sent.";
                return RedirectToAction("AdminTicketDetails", new { id = ticketId });
            }
            catch (Exception ex)
            {
                LogException(ex, "AdminAddComment");
                TempData["ErrorMessage"] = "An error occurred sending the message.";
                return RedirectToAction("AdminTicketDetails", new { id = ticketId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdminDeleteAttachment(int attachmentId, int ticketId)
        {
            try
            {
                new TicketDataAccess().DeleteAttachment(attachmentId, GetCurrentUserId());
                TempData["SuccessMessage"] = "Attachment removed.";
                return RedirectToAction("AdminTicketEdit", new { id = ticketId });
            }
            catch (Exception ex)
            {
                LogException(ex, "AdminDeleteAttachment");
                TempData["ErrorMessage"] = "An error occurred removing the attachment.";
                return RedirectToAction("AdminTicketEdit", new { id = ticketId });
            }
        }

        public ActionResult ExportTicketsCsv(string search, DateTime? date, int? assignedToUserId, string sortOrder, string view)
        {
            try
            {
                // Mirrors AllTickets' exact filtering logic so the export always matches what the admin is currently viewing
                var allTickets = adminDAL.GetAllTickets(search, null, null);

                if (date.HasValue)
                    allTickets = allTickets.Where(t => t.CreatedOn.Date == date.Value.Date).ToList();

                if (assignedToUserId.HasValue)
                    allTickets = allTickets.Where(t => t.AssignedtoUserId == assignedToUserId.Value).ToList();

                allTickets = sortOrder == "oldest"
                    ? allTickets.OrderBy(t => t.CreatedOn).ToList()
                    : allTickets.OrderByDescending(t => t.CreatedOn).ToList();

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(",", new[]
                {
            "Ticket ID", "Title", "Description", "Priority", "Status",
            "Raised By", "Assigned To", "Created On", "Closed On"
        }.Select(CsvEscape)));

                foreach (var t in allTickets)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                "TICK-" + t.TicketId.ToString("D4"),
                t.Title,
                t.Description,
                t.PriorityName ?? "Unset",
                t.StatusName,
                t.RaisedByName,
                t.AssignedToName ?? "Unassigned",
                t.CreatedOn.ToString("yyyy-MM-dd HH:mm"),
                t.TicketClosedDate.HasValue ? t.TicketClosedDate.Value.ToString("yyyy-MM-dd HH:mm") : ""
            }.Select(CsvEscape)));
                }

                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                string fileName = "AllTickets_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                LogException(ex, "ExportTicketsCsv");
                TempData["ErrorMessage"] = "An error occurred exporting tickets.";
                return RedirectToAction("AllTickets");
            }
        }

        public ActionResult ExportUsersCsv(string search)
        {
            try
            {
                // Mirrors ManageUsers' exact filtering logic
                var users = adminDAL.GetStaffUsers();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string term = search.Trim();
                    users = users.Where(u =>
                        (!string.IsNullOrEmpty(u.UserName) && u.UserName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(u.Email) && u.Email.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }

                var sb = new StringBuilder();
                sb.AppendLine(string.Join(",", new[]
                {
            "User ID", "User Name", "Email", "Role", "Status"
        }.Select(CsvEscape)));

                foreach (var u in users)
                {
                    sb.AppendLine(string.Join(",", new[]
                    {
                "USR-" + u.UserId.ToString("D4"),
                u.UserName ?? u.Email,
                u.Email,
                u.RoleName,
                u.IsActive ? "Active" : "Inactive"
            }.Select(CsvEscape)));
                }

                byte[] bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                string fileName = "Users_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                LogException(ex, "ExportUsersCsv");
                TempData["ErrorMessage"] = "An error occurred exporting users.";
                return RedirectToAction("ManageUsers");
            }
        }

        /// <summary>
        /// Escapes a single CSV field: wraps in quotes and doubles up any internal quotes
        /// if the value contains a comma, quote, or newline — standard CSV quoting rules.
        /// </summary>
        private static string CsvEscape(string field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            bool needsQuoting = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
            string escaped = field.Replace("\"", "\"\"");
            return needsQuoting ? "\"" + escaped + "\"" : escaped;
        }
    }
}