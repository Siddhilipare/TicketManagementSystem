using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TicketModel.ViewModels
{
    public class EditUserViewModel
    {
        public int UserId { get; set; }

        // Letters, spaces, hyphens, apostrophes only.
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$",
            ErrorMessage = "Name can only contain letters, spaces, hyphens, or apostrophes.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        [Range(1, 3, ErrorMessage = "Invalid role selected.")]
        public int RoleId { get; set; }

    }
}
