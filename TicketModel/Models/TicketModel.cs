using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel.Models
{
    public class TicketModel
    {
        public int TicketId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public int? PriorityId { get; set; }
        public string PriorityName { get; set; }

        public int StatusId { get; set; }
        public string StatusName { get; set; }

        public int RaisedbyUserId { get; set; }
        public string RaisedByName { get; set; }

        public int? AssignedtoUserId { get; set; }
        public string AssignedToName { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime? TicketClosedDate { get; set; }
    }
}
