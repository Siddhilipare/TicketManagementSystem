using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel
{
    public class UserDetailModel
    {
        public int UserDetailId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string City { get; set; }
       
    }
}
