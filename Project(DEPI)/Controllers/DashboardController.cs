using BookifyHotel.Data;
using BookifyHotel.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_DEPI_.Controllers;
using Project_DEPI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Project_DEPI_.Controllers
{
    // SECURITY [Admin Panel Protection / Role-Based Authorization / Principle of Least Privilege]:
    // The Dashboard is restricted to Admin and Manager roles only.
    // Regular Users and unauthenticated visitors are denied access entirely.
    [Authorize(Roles = "Admin,Manager")]
    public class DashboardController : Controller
    {
        private readonly BookifyHotelDbContext _context;

        public DashboardController(BookifyHotelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // إحصائيات الغرف
            ViewBag.TotalRooms = await _context.Rooms.CountAsync();
            ViewBag.AvailableRooms = await _context.Rooms.CountAsync(r => r.IsAvailable && r.Status == "Available");

            // إحصائيات الحجوزات
            ViewBag.TodaysBookings = await _context.Bookings
                .CountAsync(b => b.CheckInDate.Date == today);

            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == "Confirmed");

            // إحصائيات المستخدمين
            ViewBag.TotalUsers = await _context.UserProfiles.CountAsync(); // ✅ صححت من Users إلى UserProfiles

            // الإيرادات الشهرية
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            ViewBag.MonthlyRevenue = await _context.Bookings
                .Where(b => b.CheckInDate >= firstDayOfMonth && b.CheckInDate <= lastDayOfMonth && b.Status == "Confirmed")
                .SumAsync(b => (double?)b.TotalPrice) ?? 0; // ✅ أضفت null check

            // الحجوزات الحديثة
            ViewBag.RecentBookings = await _context.Bookings
                .Include(b => b.UserProfile) // ✅ صححت من User إلى UserProfile
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .OrderByDescending(b => b.CheckInDate)
                .Take(5)
                .Select(b => new DashboardBookingViewModel
                {
                    BookingId = b.BookingId,
                    FullName = b.UserProfile.FullName,
                    RoomNumber = b.Room.RoomNumber,
                    RoomType = b.Room.RoomType.Name,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice
                })
                .ToListAsync();

            return View();
        }
    }

    // ✅ ViewModel لـ Booking

}