using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel.Models
{
    public class UpdateTicketStatusViewModel
    {
        public int TicketId { get; set; }
        public int StatusId { get; set; }
        public int PriorityId { get; set; }
    }
}
