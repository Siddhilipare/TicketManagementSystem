using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketModel.Models
{
    public class UserListModel
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        public string UserName { get; set; }
        public string City { get; set; }
    }
}
