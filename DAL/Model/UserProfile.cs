// Add this to your Model folder
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookifyHotel.Model
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string IdentityUserId { get; set; }

        [ForeignKey("IdentityUserId")]
        public virtual AppUser User { get; set; }

        [StringLength(100)]
        public string FullName { get; set; }

        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(500)]
        public string ProfilePhotoPath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string Country { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        // Navigation properties
        public virtual ICollection<ReservationCart> ReservationCarts { get; set; } = new List<ReservationCart>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<User_Role> UserRoles { get; set; } = new List<User_Role>();
    }
}