using System.ComponentModel.DataAnnotations;

namespace Project_DEPI.ViewModels
{
    public class PaymentViewModel
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "usd";
        public string? Description { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public string? PublishableKey { get; set; }
        
        // Booking details for display
        public string RoomNumber { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfNights { get; set; }
        public decimal PricePerNight { get; set; }
    }

    public class PaymentRequestDto
    {
        [Required]
        public int BookingId { get; set; }
        
        [Required]
        public string PaymentIntentId { get; set; } = string.Empty;
    }

    public class CreatePaymentIntentRequest
    {
        [Required]
        public int BookingId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        public string Currency { get; set; } = "usd";
        public string? Description { get; set; }
    }

    public class PaymentRequestDTO
    {
        public string PaymentMethod { get; set; } = "card";
        public string? CardNumber { get; set; }
        public string? ExpiryDate { get; set; }
        public string? CVV { get; set; }
        public string? CardHolderName { get; set; }
        public int BookingId { get; set; }
    }
}


