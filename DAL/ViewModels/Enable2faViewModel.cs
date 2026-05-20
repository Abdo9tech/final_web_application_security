using System.ComponentModel.DataAnnotations;

namespace DAL.ViewModels
{
    public class Enable2faViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        public string? SharedKey { get; set; }

        public string? AuthenticatorUri { get; set; }
    }
}
