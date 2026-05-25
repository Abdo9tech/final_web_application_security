using Microsoft.AspNetCore.Mvc;
using RoomService = PLL.Services.RoomService;
using BookingService = PLL.Services.BookingService;
using UserService = PLL.Services.UserService;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using DAL.Constants;

namespace Project_DEPI.Controllers
{
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        private readonly RoomService _roomService;
        private readonly BookingService _bookingService;
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public DashboardController(
            RoomService roomService, 
            BookingService bookingService,
            UserService userService,
            IConfiguration configuration)
        {
            _roomService = roomService;
            _bookingService = bookingService;
            _userService = userService;
            _configuration = configuration;
        }

        // Serves the admin dashboard view
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            try
            {
                // Get room statistics
                var rooms = _roomService.GetAvailableRooms();
                var allRooms = _roomService.SearchRooms("", null, null, null);
                
                ViewBag.TotalRooms = allRooms?.Count() ?? 0;
                ViewBag.AvailableRooms = rooms?.Count() ?? 0;
                ViewBag.OccupiedRooms = ViewBag.TotalRooms - ViewBag.AvailableRooms;
                
                // Get user statistics
                ViewBag.TotalUsers = _userService.GetUsersCount();
                
                // Get booking statistics
                var allBookings = _bookingService.GetAll();
                ViewBag.TotalBookings = allBookings?.Count() ?? 0;
                
                // Calculate monthly revenue (current month)
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthlyBookings = allBookings?
                    .Where(b => b.BookingDate.Month == currentMonth && 
                               b.BookingDate.Year == currentYear &&
                               (b.Status == "Confirmed" || b.Status == "Completed"))
                    .ToList();
                ViewBag.MonthlyRevenue = monthlyBookings?.Sum(b => b.TotalPrice) ?? 0;
                
                // Get today's bookings
                var today = DateTime.Today;
                ViewBag.TodayBookings = allBookings?
                    .Count(b => b.BookingDate.Date == today) ?? 0;
                
                // Get confirmed bookings
                ViewBag.ConfirmedBookings = allBookings?
                    .Count(b => b.Status == "Confirmed" || b.Status == "Completed") ?? 0;
                
                // Get recent bookings (last 10)
                var recentBookings = _bookingService.Context.Bookings
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .Select(b => new
                    {
                        BookingId = b.BookingId,
                        FullName = b.UserProfile != null ? b.UserProfile.FullName : "Guest",
                        RoomNumber = b.Room != null ? b.Room.RoomNumber.ToString() : "N/A",
                        RoomType = b.Room != null && b.Room.RoomType != null ? b.Room.RoomType.Name : "Standard",
                        CheckInDate = b.CheckInDate,
                        Status = b.Status ?? "Pending",
                        TotalPrice = b.TotalPrice
                    })
                    .ToList();
                
                ViewBag.RecentBookings = recentBookings;
                
                // Calculate chart data for booking trends (last 6 months)
                // Only count confirmed/completed bookings for accurate trends
                var bookingTrends = new List<int>();
                for (int i = 5; i >= 0; i--)
                {
                    var targetDate = DateTime.Now.AddMonths(-i);
                    var count = allBookings?
                        .Count(b => b.BookingDate.Month == targetDate.Month && 
                                   b.BookingDate.Year == targetDate.Year &&
                                   BookingStatus.IsConfirmedOrCompleted(b.Status)) ?? 0;
                    bookingTrends.Add(count);
                }
                ViewBag.BookingTrends = bookingTrends;
                
                // Calculate revenue by room type
                // Convert to percentages for pie chart display
                var roomTypes = _roomService._context.RoomTypes.ToList();
                var revenueByType = new Dictionary<string, decimal>();
                
                // First calculate total revenue
                decimal totalRevenue = 0;
                var revenueData = new Dictionary<string, decimal>();
                
                foreach (var roomType in roomTypes)
                {
                    var revenue = allBookings?
                        .Where(b => b.Room != null && 
                                   b.Room.RoomTypeId == roomType.RoomTypeId &&
                                   BookingStatus.IsConfirmedOrCompleted(b.Status))
                        .Sum(b => b.TotalPrice) ?? 0;
                    
                    revenueData[roomType.Name] = revenue;
                    totalRevenue += revenue;
                }
                
                // Convert to percentages
                foreach (var kvp in revenueData)
                {
                    var percentage = totalRevenue > 0 
                        ? Math.Round((kvp.Value / totalRevenue) * 100, 1) 
                        : 0;
                    revenueByType[kvp.Key] = percentage;
                }
                
                ViewBag.RevenueByType = revenueByType;
                
                // Calculate weekly occupancy (last 7 days)
                // Count unique rooms occupied per day, not total bookings
                var weeklyOccupancy = new List<double>();
                for (int i = 6; i >= 0; i--)
                {
                    var targetDate = DateTime.Today.AddDays(-i);
                    
                    // Get distinct room IDs that are occupied on this date
                    var occupiedRoomIds = allBookings?
                        .Where(b => b.CheckInDate <= targetDate && 
                                   b.CheckOutDate > targetDate &&
                                   BookingStatus.IsConfirmedOrCompleted(b.Status))
                        .Select(b => b.RoomId)
                        .Distinct()
                        .Count() ?? 0;
                    
                    var occupancyRate = ViewBag.TotalRooms > 0 
                        ? Math.Round((double)occupiedRoomIds / ViewBag.TotalRooms * 100, 1) 
                        : 0;
                    
                    // Cap at 100% (in case of data issues)
                    occupancyRate = Math.Min(occupancyRate, 100);
                    
                    weeklyOccupancy.Add(occupancyRate);
                }
                ViewBag.WeeklyOccupancy = weeklyOccupancy;
                
                return View();
            }
            catch (Exception ex)
            {
                // Log the error and return a user-friendly error page
                Console.WriteLine($"Dashboard Error: {ex.Message}");
                TempData["ErrorMessage"] = "Unable to load dashboard data. Please try again.";
                return View();
            }
        }

        // JSON API to search/get rooms
        [HttpGet("rooms")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetRooms([FromQuery] string? query)
        {
            try
            {
                var rooms = _roomService.SearchRooms(query ?? string.Empty, null, null, null);
                
                if ((rooms == null || !rooms.Any()) && string.IsNullOrEmpty(query))
                {
                    rooms = _roomService.GetAvailableRooms();
                }

                var result = rooms.Select(r => new {
                    id = r.RoomId,
                    name = $"{r.RoomType?.Name ?? "Room"} #{r.RoomNumber}",
                    price = r.RoomType?.PricePerNight ?? 0,
                    img = string.IsNullOrEmpty(r.ImageUrl) 
                        ? "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop" 
                        : r.ImageUrl,
                    amenities = (r.RoomType?.Description ?? "WiFi, TV, AC")
                        .Split(new[] { ',', ';', '.' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => a.Trim())
                        .ToList(),
                    location = r.Location,
                    isAvailable = r.IsAvailable,
                    status = r.Status,
                    capacity = r.RoomType?.Capacity ?? 2
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

    }
}
