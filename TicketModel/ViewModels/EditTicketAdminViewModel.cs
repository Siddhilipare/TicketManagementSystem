using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TicketModel.ViewModels
{
    public class EditTicketAdminViewModel
    {
        public int TicketId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, MinimumLength = 3,
            ErrorMessage = "Title must be between 3 and 100 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, MinimumLength = 10,
            ErrorMessage = "Description must be between 10 and 2000 characters.")]
        public string Description { get; set; }

        // PriorityId: 1 = Low, 2 = Medium, 3 = High. Nullable — unset is allowed.
        [Range(1, 3, ErrorMessage = "Invalid priority value.")]
        public int? PriorityId { get; set; }

        // StatusId: 1 = Open, 2 = In Progress, 3 = On Hold, 4 = Closed.
        [Range(1, 4, ErrorMessage = "Invalid status value.")]
        public int StatusId { get; set; }
    }
}
