using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using TicketDAL.Dal;
using Ticket_Management_System.Helpers;

namespace Ticket_Management_System.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private NotificationDataAccess notificationDAL = new NotificationDataAccess();

        private int GetCurrentUserId()
        {
            var principal = (ClaimsPrincipal)User;
            var claim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null) throw new UnauthorizedAccessException();
            return Convert.ToInt32(claim.Value);
        }

        private void LogException(Exception ex, string actionName)
        {
            try
            {
                Helpers.Logger.LogToFile(ex, "Notification", actionName);
                new TicketDAL.Dal.ErrorLogDataAccess().LogError(
                    controllerName: "Notification",
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
                int userId = GetCurrentUserId();
                var notifications = notificationDAL.GetByUser(userId);
                return View(notifications);
            }
            catch (Exception ex)
            {
                LogException(ex, "Index");
                return View(new List<TicketDAL.Dal.NotificationItem>());
            }
        }

        [HttpPost]
        public ActionResult MarkAsRead(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "Invalid notification ID." });
            try
            {
                int userId = GetCurrentUserId();
                bool success = notificationDAL.MarkAsRead(id, userId);
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                LogException(ex, "MarkAsRead");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult MarkAllAsRead()
        {
            try
            {
                notificationDAL.MarkAllRead(GetCurrentUserId());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                LogException(ex, "MarkAllAsRead");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult GetAll()
        {
            try
            {
                int userId = GetCurrentUserId();
                var notifications = notificationDAL.GetByUser(userId);
                var result = notifications.Select(n => new {
                    id = n.Id,
                    message = n.Message,
                    ticketId = n.TicketId,
                    isRead = n.IsRead,
                    createdOn = n.CreatedOn.ToString("MMM dd, hh:mm tt")
                }).ToList();
                return Json(new { success = true, notifications = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                LogException(ex, "GetAll");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}