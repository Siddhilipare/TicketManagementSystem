using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel.Models
{
    public class TicketAttachmentModel
    {
        public int TicketAttachmentId { get; set; }
        public int TicketId { get; set; }
        public string FilePath { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }

    }
}
