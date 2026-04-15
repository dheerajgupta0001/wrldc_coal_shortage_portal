using System.ComponentModel.DataAnnotations;

namespace CoalShortagePortal.WebApp.Models
{
    public class UserCreateVM
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress] 
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "User Type")]
        public string UserType { get; set; }

        [Display(Name = "State")]
        public string State { get; set; }
    }
}
