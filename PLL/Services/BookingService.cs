using BookifyHotel.Data;
using BookifyHotel.Model;
using DAL.DataBase;
using DAL.Constants;
using Microsoft.EntityFrameworkCore;

namespace PLL.Services
{
    public class BookingService : BaseService<Booking>
    {
        private readonly BookifyHotelDbContext _context;

        public BookingService(IGenericRepository<Booking> repo, BookifyHotelDbContext context) : base(repo)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Expose context for controllers that need direct queries
        public BookifyHotelDbContext Context => _context;

        // ─── Counts ─────────────────────────────────────────────────────────────

        public int GetUserBookingsCount(int userId)
            => _repo.GetAll().Count(b => b.UserProfileId == userId);

        public int GetActiveUserBookingsCount(int userId)
            => _repo.GetAll().Count(b => b.UserProfileId == userId &&
                                         BookingStatus.IsConfirmedOrCompleted(b.Status));

        public decimal GetTotalRevenue()
            => _repo.GetAll()
                    .Where(b => BookingStatus.IsConfirmedOrCompleted(b.Status))
                    .Sum(b => b.TotalPrice);

        // ─── Retrieval ───────────────────────────────────────────────────────────

        public IEnumerable<Booking> GetBookingsByUserId(int userId)
            => _repo.GetAll()
                    .Where(b => b.UserProfileId == userId)
                    .OrderByDescending(b => b.CheckInDate)
                    .ToList();

        public async Task<List<Booking>> GetBookingsByUserIdAsync(int userId)
            => await _context.Bookings
                             .Include(b => b.Room).ThenInclude(r => r.RoomType)
                             .Include(b => b.UserProfile)
                             .Where(b => b.UserProfileId == userId)
                             .OrderByDescending(b => b.CheckInDate)
                             .ToListAsync();

        public Booking? GetBookingWithRoom(int bookingId)
            => _context.Bookings
                       .Include(b => b.Room).ThenInclude(r => r.RoomType)
                       .FirstOrDefault(b => b.BookingId == bookingId);

        public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId)
            => await _context.Bookings
                             .Include(b => b.Room).ThenInclude(r => r.RoomType)
                             .Include(b => b.UserProfile)
                             .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        // ─── Status Updates ──────────────────────────────────────────────────────

        public bool UpdateBookingStatus(int bookingId, string status)
        {
            var booking = _repo.GetById(bookingId);
            if (booking == null) return false;

            booking.Status = BookingStatus.Normalize(status);
            booking.UpdatedAt = DateTime.Now;
            _repo.Update(booking);
            _repo.Save();
            return true;
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
        {
            var booking = _repo.GetById(bookingId);
            if (booking == null) return false;

            booking.Status = BookingStatus.Normalize(status);
            booking.UpdatedAt = DateTime.Now;
            _repo.Update(booking);
            await _repo.SaveAsync();
            return true;
        }

        public async Task<bool> CancelBookingAsync(int bookingId)
        {
            var booking = _repo.GetById(bookingId);
            if (booking == null) return false;
            if (BookingStatus.IsCancelled(booking.Status) || BookingStatus.IsCompleted(booking.Status))
                return false;

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.Now;
            _repo.Update(booking);
            await _repo.SaveAsync();
            return true;
        }

        // ─── Availability ────────────────────────────────────────────────────────

        public bool IsRoomAvailableForDates(int roomId, DateTime checkIn, DateTime checkOut)
            => !_repo.GetAll()
                     .Any(b => b.RoomId == roomId &&
                               BookingStatus.IsConfirmed(b.Status) &&
                               ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                                (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                                (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)));

        public async Task<bool> IsRoomAvailableForDatesAsync(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var hasConflict = await _context.Bookings
                .AnyAsync(b => b.RoomId == roomId &&
                               b.Status == "Confirmed" &&
                               ((checkIn >= b.CheckInDate && checkIn < b.CheckOutDate) ||
                                (checkOut > b.CheckInDate && checkOut <= b.CheckOutDate) ||
                                (checkIn <= b.CheckInDate && checkOut >= b.CheckOutDate)));
            return !hasConflict;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        public int CalculateNights(DateTime checkIn, DateTime checkOut)
        {
            var nights = (checkOut - checkIn).Days;
            return nights < 1 ? 1 : nights;
        }

        public (bool IsValid, string ErrorMessage) ValidateBookingDates(DateTime checkIn, DateTime checkOut)
        {
            if (checkIn < DateTime.Today)
                return (false, "Check-in date cannot be in the past");
            if (checkOut <= checkIn)
                return (false, "Check-out date must be after check-in date");
            if ((checkOut - checkIn).Days > 365)
                return (false, "Booking cannot exceed 365 nights");
            return (true, string.Empty);
        }

        // ─── Maintenance ─────────────────────────────────────────────────────────

        public async Task<int> AutoCompleteExpiredBookingsAsync()
        {
            var yesterday = DateTime.Today.AddDays(-1);
            var expired = await _context.Bookings
                .Where(b => b.Status == "Confirmed" && b.CheckOutDate <= yesterday)
                .ToListAsync();

            foreach (var b in expired)
            {
                b.Status = BookingStatus.Completed;
                b.UpdatedAt = DateTime.Now;
            }

            if (expired.Any()) await _context.SaveChangesAsync();
            return expired.Count;
        }
    }
}
