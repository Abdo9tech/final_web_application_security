using System.ComponentModel.DataAnnotations;

namespace BookifyHotel.Model
{
    public class Payment
    {
        public int PaymentId { get; set; }
        
        [Required]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; } = "card";

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string PaymentStatus { get; set; } = "pending"; // pending, succeeded, failed, cancelled

        public string? TransactionReference { get; set; }
        
        public string? PaymentIntentId { get; set; } // Stripe Payment Intent ID
        
        public string? Currency { get; set; } = "usd";
        
        public string? CustomerId { get; set; } // Stripe Customer ID
        
        public string? ReceiptUrl { get; set; }

        [Required]
        public int BookingId { get; set; }

        public virtual Booking? Booking { get; set; }
        public string TransactionRefrence { get; set; }
    }
}
