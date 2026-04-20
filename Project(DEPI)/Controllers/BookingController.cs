using BookifyHotel.Data;
using BookifyHotel.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLL.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Project_DEPI.ViewModels;

namespace Project_DEPI_.Controllers
{
    public class BookingController : Controller
    {
        private readonly BookingService _bookingService;
        private readonly UserService _userService;
        private readonly RoomService _roomService;
        private readonly RoomTypeService _roomTypeService;
        private readonly BookifyHotelDbContext _context;

        public BookingController(
            BookingService bookingService,
            UserService userService,
            RoomService roomService,
            RoomTypeService roomTypeService,
            BookifyHotelDbContext context)
        {
            _bookingService = bookingService;
            _userService = userService;
            _roomService = roomService;
            _roomTypeService = roomTypeService;
            _context = context;
        }





        #region Admin Dashboard  CRUD Operations
        
        // SECURITY [Admin Panel Protection / Role-Based Authorization]:
        // Administrative actions are restricted strictly to users with Admin or Manager roles.
        // The default fallback policy also requires authentication.
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Index()
        {
             
            var Booking = _bookingService.GetAll(); 
            return View(Booking);
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Details(int id)
        {
            var booking = _bookingService.GetById(id);
            if (booking == null)
            {
                return NotFound();
            }
            return View(booking);
        }
        
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var booking = _bookingService.GetById(id);
            if (booking == null)
            {
                return NotFound();
            }
            else
           {
                _bookingService.Delete(id);
                return RedirectToAction("Index");
            }

        }












        // GET: Booking/Create
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            try
            {
                // الحصول على المستخدمين الحاليين
                var users = await _context.UserProfiles
                    .Select(u => new { u.Id, u.FullName, u.Email })
                    .ToListAsync();

                var userList = users.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.FullName} ({u.Email})"
                }).ToList();

                // الحصول على الغرف المتاحة
                var availableRooms = await _context.Rooms
                    .Where(r => r.IsAvailable && r.Status == "Available")
                    .Include(r => r.RoomType)
                    .Select(r => new
                    {
                        r.RoomId,
                        r.RoomNumber,
                        RoomType = r.RoomType.Name,
                        PricePerNight = r.RoomType.PricePerNight
                    })
                    .ToListAsync();

                var roomList = availableRooms.Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = $"Room {r.RoomNumber} - {r.RoomType} (${r.PricePerNight:N2}/night)"
                }).ToList();

                ViewBag.Users = new SelectList(userList, "Value", "Text");
                ViewBag.Rooms = new SelectList(roomList, "Value", "Text");
                ViewBag.TodayDate = DateTime.Now.ToString("yyyy-MM-dd");

                // تمرير بيانات الغرف لاستخدامها في JavaScript
                ViewBag.RoomsData = availableRooms.ToDictionary(
                    r => r.RoomId.ToString(),
                    r => new { r.RoomType, r.PricePerNight });

                return View(new Booking());
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading data: {ex.Message}";
                return View(new Booking());
            }
        }

        // SECURITY [CSRF Protection]: Validation token ensures requests originate from our own application.
        // SECURITY [Admin Panel Protection]: Only Admins or Managers can forcibly create new bookings.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(Booking booking)
        {
            try
            {
                // SECURITY [Data Annotation Validation]: Ensure all constraints are met before database operations.
                if (ModelState.IsValid)
                {
                    // التحقق من توافر الغرفة
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.RoomId == booking.RoomId);

                    if (room == null)
                    {
                        ModelState.AddModelError("RoomId", "Selected room not found.");
                        return await LoadCreateViewData(booking);
                    }

                    if (!room.IsAvailable)
                    {
                        ModelState.AddModelError("RoomId", "Selected room is not available.");
                        return await LoadCreateViewData(booking);
                    }

                    // التحقق من تعارض التواريخ مع حجوزات أخرى
                    var conflictingBooking = await _context.Bookings
                        .Where(b => b.RoomId == booking.RoomId && b.Status != "Cancelled")
                        .Where(b => (booking.CheckInDate >= b.CheckInDate && booking.CheckInDate < b.CheckOutDate) ||
                                   (booking.CheckOutDate > b.CheckInDate && booking.CheckOutDate <= b.CheckOutDate) ||
                                   (booking.CheckInDate <= b.CheckInDate && booking.CheckOutDate >= b.CheckOutDate))
                        .FirstOrDefaultAsync();

                    if (conflictingBooking != null)
                    {
                        ModelState.AddModelError("", $"Room is already booked from {conflictingBooking.CheckInDate:MMM dd} to {conflictingBooking.CheckOutDate:MMM dd}");
                        return await LoadCreateViewData(booking);
                    }

                    // حساب السعر الكلي
                    var nights = (int)Math.Ceiling((booking.CheckOutDate - booking.CheckInDate).TotalDays);
                    booking.TotalPrice = room.RoomType.PricePerNight * nights;
                    booking.BookingDate = DateTime.Now;
                    booking.Status = booking.Status ?? "Pending";

                    // حفظ الحجز
                    _bookingService.Create(booking);

                    // تحديث حالة الغرفة إذا كانت الحجز مؤكدة
                    if (booking.Status == "Confirmed")
                    {
                        room.IsAvailable = false;
                        room.Status = "Booked";
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = $"Booking created successfully! Total: ${booking.TotalPrice:N2}";
                    return RedirectToAction("Index");
                }

                return await LoadCreateViewData(booking);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating booking: {ex.Message}");
                return await LoadCreateViewData(booking);
            }
        }

        // GET: Booking/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.UserProfile)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null)
                {
                    TempData["Error"] = "Booking not found.";
                    return RedirectToAction("Index");
                }

                // الحصول على المستخدمين
                var users = await _context.UserProfiles
                    .Select(u => new { u.Id, u.FullName, u.Email })
                    .ToListAsync();

                var userList = users.Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = $"{u.FullName} ({u.Email})",
                    Selected = u.Id == booking.UserProfileId
                }).ToList();

                // الحصول على جميع الغرف (بما فيها المحجوزة)
                var rooms = await _context.Rooms
                    .Include(r => r.RoomType)
                    .Select(r => new
                    {
                        r.RoomId,
                        r.RoomNumber,
                        RoomType = r.RoomType.Name,
                        PricePerNight = r.RoomType.PricePerNight,
                        IsAvailable = r.IsAvailable
                    })
                    .ToListAsync();

                var roomList = rooms.Select(r => new SelectListItem
                {
                    Value = r.RoomId.ToString(),
                    Text = $"Room {r.RoomNumber} - {r.RoomType} (${r.PricePerNight:N2}/night)" +
                           (!r.IsAvailable && r.RoomId != booking.RoomId ? " [Not Available]" : ""),
                    Selected = r.RoomId == booking.RoomId
                }).ToList();

                ViewBag.Users = new SelectList(userList, "Value", "Text");
                ViewBag.Rooms = new SelectList(roomList, "Value", "Text");
                ViewBag.TodayDate = DateTime.Now.ToString("yyyy-MM-dd");

                // تمرير بيانات الغرف لاستخدامها في JavaScript
                ViewBag.RoomsData = rooms.ToDictionary(
                    r => r.RoomId.ToString(),
                    r => new { r.RoomType, r.PricePerNight });

                return View(booking);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading booking: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // SECURITY [CSRF Protection / Role-Based Authorization]: Modifying an existing booking
        // requires an active anti-forgery token and explicit administrative roles.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Booking booking)
        {
            try
            {
                // SECURITY [Data Annotation Validation]: Basic validation passed before logic execution.
                if (ModelState.IsValid)
                {
                    // الحصول على الحجز الحالي
                    var existingBooking = await _context.Bookings
                        .Include(b => b.Room)
                        .ThenInclude(r => r.RoomTypes)
                        .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);

                    if (existingBooking == null)
                    {
                        TempData["Error"] = "Booking not found.";
                        return RedirectToAction("Index");
                    }

                    // التحقق من توافر الغرفة إذا تم تغييرها
                    if (existingBooking.RoomId != booking.RoomId)
                    {
                        var newRoom = await _context.Rooms
                            .Include(r => r.RoomType)
                            .FirstOrDefaultAsync(r => r.RoomId == booking.RoomId);

                        if (newRoom == null || (!newRoom.IsAvailable && booking.Status != "Cancelled"))
                        {
                            ModelState.AddModelError("RoomId", "Selected room is not available.");
                            return await LoadEditViewData(booking);
                        }
                    }

                    // التحقق من تعارض التواريخ
                    var conflictingBooking = await _context.Bookings
                        .Where(b => b.BookingId != booking.BookingId && b.RoomId == booking.RoomId && b.Status != "Cancelled")
                        .Where(b => (booking.CheckInDate >= b.CheckInDate && booking.CheckInDate < b.CheckOutDate) ||
                                   (booking.CheckOutDate > b.CheckInDate && booking.CheckOutDate <= b.CheckOutDate) ||
                                   (booking.CheckInDate <= b.CheckInDate && booking.CheckOutDate >= b.CheckOutDate))
                        .FirstOrDefaultAsync();

                    if (conflictingBooking != null)
                    {
                        ModelState.AddModelError("", $"Room is already booked from {conflictingBooking.CheckInDate:MMM dd} to {conflictingBooking.CheckOutDate:MMM dd}");
                        return await LoadEditViewData(booking);
                    }

                    // تحديث بيانات الحجز
                    existingBooking.UserProfileId = booking.UserProfileId;
                    existingBooking.RoomId = booking.RoomId;
                    existingBooking.CheckInDate = booking.CheckInDate;
                    existingBooking.CheckOutDate = booking.CheckOutDate;
                    existingBooking.Status = booking.Status;

                    // حساب السعر الجديد
                    var room = await _context.Rooms
                        .Include(r => r.RoomType)
                        .FirstOrDefaultAsync(r => r.RoomId == booking.RoomId);

                    if (room != null)
                    {
                        var nights = (int)Math.Ceiling((booking.CheckOutDate - booking.CheckInDate).TotalDays);
                        existingBooking.TotalPrice = room.RoomType.PricePerNight * nights;
                    }

                    // تحديث حالة الغرف
                    await UpdateRoomStatus(existingBooking.RoomId, booking.Status);

                    // حفظ التعديلات
                    _bookingService.Update(existingBooking);

                    TempData["Success"] = "Booking updated successfully!";
                    return RedirectToAction("Details", new { id = booking.BookingId });
                }

                return await LoadEditViewData(booking);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating booking: {ex.Message}");
                return await LoadEditViewData(booking);
            }
        }

        // دالة مساعدة لتحميل بيانات العرض في Create
        private async Task<ViewResult> LoadCreateViewData(Booking booking)
        {
            var users = await _context.UserProfiles
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            var userList = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.FullName} ({u.Email})"
            }).ToList();

            var rooms = await _context.Rooms
                .Where(r => r.IsAvailable)
                .Include(r => r.RoomType)
                .Select(r => new
                {
                    r.RoomId,
                    r.RoomNumber,
                    RoomType = r.RoomType.Name,
                    PricePerNight = r.RoomType.PricePerNight
                })
                .ToListAsync();

            var roomList = rooms.Select(r => new SelectListItem
            {
                Value = r.RoomId.ToString(),
                Text = $"Room {r.RoomNumber} - {r.RoomType} (${r.PricePerNight:N2}/night)"
            }).ToList();

            ViewBag.Users = new SelectList(userList, "Value", "Text");
            ViewBag.Rooms = new SelectList(roomList, "Value", "Text");
            ViewBag.TodayDate = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.RoomsData = rooms.ToDictionary(
                r => r.RoomId.ToString(),
                r => new { r.RoomType, r.PricePerNight });

            return View(booking);
        }

        // دالة مساعدة لتحميل بيانات العرض في Edit
        private async Task<ViewResult> LoadEditViewData(Booking booking)
        {
            var existingBooking = await _context.Bookings
                .Include(b => b.UserProfile)
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomTypes)
                .FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);

            if (existingBooking == null)
            {
                TempData["Error"] = "Booking not found.";
                return View(booking);
            }

            // تحديث البيانات من الـ Model على existingBooking
            existingBooking.UserProfileId = booking.UserProfileId;
            existingBooking.RoomId = booking.RoomId;
            existingBooking.CheckInDate = booking.CheckInDate;
            existingBooking.CheckOutDate = booking.CheckOutDate;
            existingBooking.Status = booking.Status;

            var users = await _context.UserProfiles
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToListAsync();

            var userList = users.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.FullName} ({u.Email})",
                Selected = u.Id == booking.UserProfileId
            }).ToList();

            var rooms = await _context.Rooms
                .Include(r => r.RoomTypes)
                .Select(r => new
                {
                    r.RoomId,
                    r.RoomNumber,
                    RoomType = r.RoomTypes.Name,
                    PricePerNight = r.RoomTypes.PricePerNight,
                    IsAvailable = r.IsAvailable
                })
                .ToListAsync();

            var roomList = rooms.Select(r => new SelectListItem
            {
                Value = r.RoomId.ToString(),
                Text = $"Room {r.RoomNumber} - {r.RoomType} (${r.PricePerNight:N2}/night)" +
                       (!r.IsAvailable && r.RoomId != booking.RoomId ? " [Not Available]" : ""),
                Selected = r.RoomId == booking.RoomId
            }).ToList();

            ViewBag.Users = new SelectList(userList, "Value", "Text");
            ViewBag.Rooms = new SelectList(roomList, "Value", "Text");
            ViewBag.TodayDate = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.RoomsData = rooms.ToDictionary(
                r => r.RoomId.ToString(),
                r => new { r.RoomType, r.PricePerNight });

            return View(existingBooking);
        }

        // دالة لتحديث حالة الغرفة
        private async Task UpdateRoomStatus(int roomId, string bookingStatus)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                if (bookingStatus == "Confirmed")
                {
                    room.IsAvailable = false;
                    room.Status = "Booked";
                }
                else if (bookingStatus == "Cancelled" || bookingStatus == "Completed")
                {
                    room.IsAvailable = true;
                    room.Status = "Available";
                }
                await _context.SaveChangesAsync();
            }
        }
        #endregion


        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.IdentityUserId == userId);
            
            if (userProfile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Where(b => b.UserProfileId == userProfile.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }
    }
}

