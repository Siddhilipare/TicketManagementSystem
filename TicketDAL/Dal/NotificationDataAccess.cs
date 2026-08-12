using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using Microsoft.Practices.EnterpriseLibrary.Data;

namespace TicketDAL.Dal
{
    public class NotificationItem
    {
        public int NotificationId { get; set; }
        public int Id { get { return NotificationId; } set { NotificationId = value; } }
        public string Message { get; set; }
        public int? TicketId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class NotificationDataAccess
    {
        private Database db;
        public NotificationDataAccess() { db = DatabaseFactory.CreateDatabase(); }

        public void Insert(int userId, string message, int? ticketId = null)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Notification_Insert");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.AddInParameter(cmd, "@Message", DbType.String, message);
                db.AddInParameter(cmd, "@TicketId", DbType.Int32, (object)ticketId ?? DBNull.Value);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "Insert");
                throw;
            }

            // Mirror every in-app notification to the recipient's registered email.
            try
            {
                var user = new UserDAL().GetUserById(userId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    string ticketRef = ticketId.HasValue
                        ? "Ticket ID: <strong>TICK-" + ticketId.Value.ToString("D4") + "</strong>"
                        : "";
                    string body = BuildNotificationHtml(message, ticketRef);
                    EmailHelper.Send(user.Email, "New notification from Simplify IT Support", body);
                }
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "InsertEmail");
            }
        }

        private static string BuildNotificationHtml(string message, string ticketRef)
        {
            return @"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 20px;'>
  <div style='max-width: 480px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
    <div style='background: #1a73e8; padding: 28px 32px;'>
      <div style='font-size: 22px; font-weight: 700; color: #ffffff;'>Simplify</div>
      <div style='font-size: 13px; color: rgba(255,255,255,0.8); margin-top: 4px;'>IT Ticket Management</div>
    </div>
    <div style='padding: 32px;'>
      <h2 style='font-size: 20px; color: #1a1a1a; margin: 0 0 12px 0;'>You have a new notification</h2>
      <p style='font-size: 14px; color: #555; line-height: 1.6; margin: 0 0 16px 0;'>" + message + @"</p>
      " + (string.IsNullOrEmpty(ticketRef) ? "" : "<p style='font-size: 14px; color: #333; margin: 0 0 16px 0;'>" + ticketRef + "</p>") + @"
      <p style='font-size: 12px; color: #999; margin-top: 24px; line-height: 1.5;'>
        Sign in to Simplify to view the full details.
      </p>
      <hr style='border: none; border-top: 1px solid #eee; margin: 24px 0 16px;'/>
      <div style='font-size: 11px; color: #bbb;'>Simplify &mdash; IT Support System</div>
    </div>
  </div>
</body>
</html>";
        }

        public List<NotificationItem> GetByUser(int userId)
        {
            try
            {
                var list = new List<NotificationItem>();
                DbCommand cmd = db.GetStoredProcCommand("Notification_GetByUser");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read())
                    {
                        list.Add(new NotificationItem
                        {
                            NotificationId = Convert.ToInt32(dr["NotificationId"]),
                            Message = dr["Message"].ToString(),
                            TicketId = dr["TicketId"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["TicketId"]),
                            IsRead = Convert.ToBoolean(dr["IsRead"]),
                            CreatedOn = Convert.ToDateTime(dr["CreatedOn"])
                        });
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "GetByUser");
                throw;
            }
        }

        public int GetUnreadCount(int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Notification_GetUnreadCount");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                return Convert.ToInt32(db.ExecuteScalar(cmd));
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "GetUnreadCount");
                throw;
            }
        }

        public void MarkAllRead(int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Notification_MarkAllRead");
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.ExecuteNonQuery(cmd);
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "MarkAllRead");
                throw;
            }
        }

        public bool MarkAsRead(int notificationId, int userId)
        {
            try
            {
                DbCommand cmd = db.GetStoredProcCommand("Notification_MarkAsRead");
                db.AddInParameter(cmd, "@NotificationId", DbType.Int32, notificationId);
                db.AddInParameter(cmd, "@UserId", DbType.Int32, userId);
                db.ExecuteNonQuery(cmd);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "MarkAsRead");
                throw;
            }
        }

        public List<int> GetAllAdminUserIds()
        {
            try
            {
                var ids = new List<int>();
                DbCommand cmd = db.GetStoredProcCommand("User_GetAllAdmins");
                using (IDataReader dr = db.ExecuteReader(cmd))
                {
                    while (dr.Read()) ids.Add(Convert.ToInt32(dr["UserId"]));
                }
                return ids;
            }
            catch (Exception ex)
            {
                Logger.LogToFile(ex, "NotificationDataAccess", "GetAllAdminUserIds");
                throw;
            }
        }
    }
}
