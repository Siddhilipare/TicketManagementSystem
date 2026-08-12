using System.Collections.Generic;
using TicketDAL.Dal;

namespace Ticket_Management_System.Helpers
{
    public static class CommentNotifier
    {
        public static void NotifyStakeholders(int ticketId, string ticketTitle, int commenterUserId, string commenterRole)
        {
            var ticketDAL = new TicketDataAccess();
            var notifyDAL = new NotificationDataAccess();

            int raisedByUserId;
            int? assignedToUserId;
            ticketDAL.GetStakeholders(ticketId, out raisedByUserId, out assignedToUserId);

            var recipients = new HashSet<int>();
            recipients.Add(raisedByUserId);
            if (assignedToUserId.HasValue) recipients.Add(assignedToUserId.Value);
            foreach (var adminId in notifyDAL.GetAllAdminUserIds()) recipients.Add(adminId);
            recipients.Remove(commenterUserId);

            string message = commenterRole + " added a comment on \"" + ticketTitle + "\"";
            foreach (var userId in recipients)
                notifyDAL.Insert(userId, message, ticketId);
        }
    }
}