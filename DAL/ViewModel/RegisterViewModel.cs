using System.ComponentModel.DataAnnotations;

namespace BookifyHotel.ViewModels
{
    // SECURITY [Input Validation]: All fields use strict server-side data annotations.
    // This prevents malformed or overly long inputs from reaching business logic.
    public class RegisterViewModel
    {
        // SECURITY [Input Validation]: FullName is limited to 100 chars to prevent buffer abuse.
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // SECURITY [Input Validation]: Email is validated as a properly formatted address.
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        // SECURITY [Input Validation]: Phone number is limited and format-validated.
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [StringLength(15, MinimumLength = 7, ErrorMessage = "Phone number must be between 7 and 15 characters")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        // SECURITY [Strong Password Policy]: Password requires minimum 8 characters.
        // Additional complexity (digit, uppercase, symbol) is enforced by Identity in Program.cs.
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        // SECURITY [Input Validation]: Confirm password must match Password exactly.
        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must agree to the terms and conditions")]
        [Display(Name = "I agree to Terms and Conditions")]
        public bool AgreeToTerms { get; set; }
    }
}

