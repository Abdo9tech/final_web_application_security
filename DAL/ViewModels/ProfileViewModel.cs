using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DAL.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(15, ErrorMessage = "Phone number cannot be longer than 15 characters.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhoto { get; set; }
        
        public string? ProfilePhotoPath { get; set; }
    }
}
