using System.ComponentModel.DataAnnotations;

namespace Project_DEPI.ViewModels
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        
        [Required]
        public int RoomId { get; set; }
        
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Check-in Date")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        [Display(Name = "Check-out Date")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }
        
        [Required]
        [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10")]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; } = 1;
        
        [Display(Name = "Special Requests")]
        public string? SpecialRequests { get; set; }
        
        public decimal PricePerNight { get; set; }
        public decimal TotalPrice { get; set; }
        public int NumberOfNights { get; set; }
        
        // Payment Information
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "card";
        
        // Validation properties
        public DateTime MinCheckInDate { get; set; } = DateTime.Today.AddDays(1);
        public DateTime MaxCheckOutDate { get; set; } = DateTime.Today.AddDays(365);
        public int MaxGuests { get; set; } = 4;
        
        // Computed properties
        public bool IsValid => CheckInDate < CheckOutDate && CheckInDate >= DateTime.Today;
    }
}