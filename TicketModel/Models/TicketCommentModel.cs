using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel.Models
{
    public class TicketCommentModel
    {
        public int TicketCommentId { get; set; }
        public int TicketId { get; set; }
        public int CommentedbyUserId { get; set; }
        public string CommentedByName { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
