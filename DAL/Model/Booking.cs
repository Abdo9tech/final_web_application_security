using BookifyHotel.Model;
using System.ComponentModel.DataAnnotations;

namespace BookifyHotel.Model
{
    public class Booking
    {
        public int BookingId { get; set; }

        public int UserProfileId { get; set; }
        
        [Required]
        public int RoomId { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public string Status { get; set; } = "pending"; // pending, confirmed, cancelled, completed

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public int NumberOfGuests { get; set; } = 1;
        
        public string? SpecialRequests { get; set; }
        
        public string? PaymentIntentId { get; set; } // For Stripe integration

        // Navigation properties
        public virtual UserProfile UserProfile { get; set; } = null!;
        public virtual Room Room { get; set; } = null!;
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        // Computed properties
        public int NumberOfNights => (int)(CheckOutDate - CheckInDate).TotalDays;
        public bool IsActive => Status == "confirmed" || Status == "pending";
        public bool CanBeCancelled => Status == "confirmed" && CheckInDate > DateTime.UtcNow.AddDays(1);
        
        // Compatibility property for existing code
        public int UserId { get; set; }
    }
}
