using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace TicketModel.ViewModels
{
   public class AddStaffUserViewModel
    {
       
            // Letters, spaces, hyphens, apostrophes only. No digits, no symbols.
            [Required(ErrorMessage = "Name is required.")]
            [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters.")]
            [RegularExpression(@"^[a-zA-Z\s\-']+$",
                ErrorMessage = "Name can only contain letters, spaces, hyphens, or apostrophes. No numbers or symbols.")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 8,
                ErrorMessage = "Password must be at least 8 characters.")]
            public string Password { get; set; }

            // RoleId 1 = Administrator, 2 = Support Executive, 3 = Employee
            [Required(ErrorMessage = "Please select a role.")]
            [Range(1, 3, ErrorMessage = "Invalid role selected.")]
            public int RoleId { get; set; }
        }
}
