using System.ComponentModel.DataAnnotations;

namespace Project_DEPI.ViewModels
{
    // SECURITY [ViewModel Isolation / Overposting Protection]:
    // This dedicated ViewModel ensures that only safe, expected fields are mapped from HTTP requests.
    // It prevents attackers from modifying sensitive internal properties of the domain model using Overposting.
    public class BookNowViewModel
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public string? RoomDescription { get; set; }
        public List<string>? RoomAmenities { get; set; }
        public string? RoomImageUrl { get; set; }
        // SECURITY [Data Annotation Validation]: Strict validation rules are applied at the
        // model level to ensure valid input types, mandatory values, and limits (e.g., Dates, ranges).
        [Required(ErrorMessage = "Check-in date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in Date")]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Check-out date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out Date")]
        public DateTime CheckOutDate { get; set; }

        [Range(1, 10, ErrorMessage = "Number of guests must be between 1 and 10")]
        [Display(Name = "Number of Guests")]
        public int NumberOfGuests { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Special requests cannot exceed 500 characters")]
        [Display(Name = "Special Requests")]
        public string? SpecialRequests { get; set; }

        // Calculated properties
        public int NumberOfNights => (CheckOutDate - CheckInDate).Days > 0 ? (CheckOutDate - CheckInDate).Days : 0;
        public decimal TotalPrice => NumberOfNights * PricePerNight;
        public decimal TaxAmount => TotalPrice * 0.1m; // 10% tax
        public decimal GrandTotal => TotalPrice + TaxAmount;

        // Payment
        public string PublishableKey { get; set; } = string.Empty;
        public bool ProcessPayment { get; set; } = true;

        // Validation properties
        public DateTime MinCheckInDate { get; set; } = DateTime.Today.AddDays(1);
        public DateTime MaxCheckOutDate { get; set; } = DateTime.Today.AddDays(365);
    }
}




