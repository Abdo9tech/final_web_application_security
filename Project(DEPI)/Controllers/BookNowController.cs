using Stripe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Project_DEPI.Services;
using Project_DEPI.ViewModels;
using BookifyHotel.Data;
using BookifyHotel.Model;
using PLL.Services;

namespace Project_DEPI_.Controllers
{
    [Authorize]
    public class BookNowController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly BookifyHotelDbContext _context;
        private readonly RoomService _roomService;
        private readonly BookingService _bookingService;

        public BookNowController(
            IPaymentService paymentService, 
            BookifyHotelDbContext context,
            RoomService roomService,
            BookingService bookingService)
        {
            _paymentService = paymentService;
            _context = context;
            _roomService = roomService;
            _bookingService = bookingService;
        }

        // GET: BookNow/Index/{id} or BookNow/Index?roomId={id} - Main booking page
        [HttpGet]
        public async Task<IActionResult> Index(int? id, int? roomId)
        {
            var actualRoomId = id ?? roomId ?? 0;
            if (actualRoomId == 0)
            {
                TempData["Error"] = "Room ID is required";
                return RedirectToAction("Index", "Room");
            }
            return await Room(actualRoomId);
        }

        // GET: BookNow/Room/{id} - Main booking page
        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == id);

            if (room == null || !room.IsAvailable)
            {
                TempData["Error"] = "Room is not available for booking";
                return RedirectToAction("Index", "Room");
            }

            var viewModel = new BookNowViewModel
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomNumber.ToString(),
                RoomType = room.RoomType?.Name ?? "Standard",
                PricePerNight = room.RoomType?.PricePerNight ?? 0,
                RoomDescription = "Comfortable room with modern amenities",
                CheckInDate = DateTime.Today.AddDays(1),
                CheckOutDate = DateTime.Today.AddDays(2),
                PublishableKey = _paymentService.GetPublishableKey()
            };

            return View(viewModel);
        }

        // SECURITY [Server-Side Input Validation / Overposting Protection]:
        // ViewModels are used defensively (ViewModel Isolation). 
        // Only fields explicitly defined in BookNowViewModel bind to the model, preventing
        // attackers from overposting malicious data to alter entity properties.
        // SECURITY [CSRF Protection]: ValidateAntiForgeryToken is active globally and explicitly.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBooking(BookNowViewModel model)
        {
            try
            {
                // SECURITY [Data Annotation Validation]: Validates required fields, explicit date ranges,
                // and data formats defined in the ViewModel before any processing occurs.
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "Please fill in all required fields correctly. Details: " + string.Join(", ", errors);
                    return View("Index", model);
                }

            var userId = GetCurrentUserId();
            
            // Get user profile
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

                if (userProfile == null)
                {
                    ModelState.AddModelError("", "User profile not found. Please complete your profile first.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "User profile not found. Please complete your profile first.";
                    return View("Index", model);
                }

            // Re-fetch room from database to get trusted PricePerNight and ensure availability
            var room = await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

                if (room == null || !room.IsAvailable)
                {
                    ModelState.AddModelError("", "Selected room is no longer available.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "Selected room is no longer available.";
                    return View("Index", model);
                }

                // SECURITY [Server-Side Input Validation]: All business logic dates and financial 
                // data are re-validated server-side, never trusting the client's calculations.
                if (model.CheckOutDate <= model.CheckInDate)
                {
                    ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "Check-out date must be after check-in date.";
                    return View("Index", model);
                }

                if (model.CheckInDate < DateTime.Today)
                {
                    ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "Check-in date cannot be in the past.";
                    return View("Index", model);
                }

            // Check for date conflicts
            var hasConflict = await _context.Bookings
                .AnyAsync(b => b.RoomId == model.RoomId && 
                              b.Status != "Cancelled" &&
                              ((model.CheckInDate >= b.CheckInDate && model.CheckInDate < b.CheckOutDate) ||
                               (model.CheckOutDate > b.CheckInDate && model.CheckOutDate <= b.CheckOutDate) ||
                               (model.CheckInDate <= b.CheckInDate && model.CheckOutDate >= b.CheckOutDate)));

                if (hasConflict)
                {
                    ModelState.AddModelError("", "Room is not available for the selected dates.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "Room is not available for the selected dates.";
                    return View("Index", model);
                }

                // SECURITY [Data Isolation / Payment Tampering Protection]: Re-calculate
                // price exclusively on the server side using TRUSTED database records.
                // The client-submitted `GrandTotal` is discarded entirely to prevent price manipulation.
                model.PricePerNight = room.RoomType?.PricePerNight ?? 0;
                var grandTotal = model.GrandTotal; // This will use the new PricePerNight and validated dates
                
                // Create booking record
                var booking = new Booking
                {
                    UserProfileId = userProfile.Id,
                    RoomId = model.RoomId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    TotalPrice = grandTotal,
                    Status = "Pending",
                    BookingDate = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Create payment intent
                var metadata = new Dictionary<string, string>
                {
                    { "booking_id", booking.BookingId.ToString() },
                    { "user_id", userId },
                    { "room_id", model.RoomId.ToString() }
                };

                var paymentIntent = await _paymentService.CreatePaymentIntentAsync(
                    grandTotal,
                    "usd",
                    $"Hotel Booking #{booking.BookingId} - Room {room.RoomNumber}",
                    metadata
                );

                // Update booking with payment intent ID (if the field exists)
                booking.PaymentIntentId = paymentIntent.Id;
                await _context.SaveChangesAsync();

                // Redirect to payment page
                return RedirectToAction("Payment", new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while processing your booking: {ex.Message}");
                model.PublishableKey = _paymentService.GetPublishableKey();
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View("Index", model);
            }
        }

        // GET: BookNow/Payment/{bookingId} - Payment page
        [HttpGet]
        public async Task<IActionResult> Payment(int bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.UserProfile)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && 
                                         b.UserProfile.IdentityUserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found";
                return RedirectToAction("Index", "Home");
            }

            if (booking.Status != "Pending")
            {
                TempData["Error"] = "This booking cannot be paid";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            // Retrieve or create payment intent
            PaymentIntent paymentIntent;
            if (!string.IsNullOrEmpty(booking.PaymentIntentId))
            {
                try
                {
                    paymentIntent = await _paymentService.GetPaymentIntentAsync(booking.PaymentIntentId);
                    
                    // If the amount has changed, we should update it, but for now we'll just ensure it exists
                    // Optional: Update payment intent if booking.TotalPrice != paymentIntent.Amount / 100
                }
                catch (Exception)
                {
                    // If retrieval fails (e.g. invalid ID), create a new one
                    paymentIntent = await _paymentService.CreatePaymentIntentAsync(
                        booking.TotalPrice,
                        "usd",
                        $"Hotel Booking #{booking.BookingId}"
                    );
                    booking.PaymentIntentId = paymentIntent.Id;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Create a new payment intent
                paymentIntent = await _paymentService.CreatePaymentIntentAsync(
                    booking.TotalPrice,
                    "usd",
                    $"Hotel Booking #{booking.BookingId}"
                );
                booking.PaymentIntentId = paymentIntent.Id;
                await _context.SaveChangesAsync();
            }

            var viewModel = new PaymentViewModel
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalPrice,
                RoomNumber = booking.Room.RoomNumber.ToString(),
                RoomType = booking.Room.RoomType?.Name ?? "Standard",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days,
                PricePerNight = booking.Room.RoomType?.PricePerNight ?? 0,
                PublishableKey = _paymentService.GetPublishableKey(),
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var booking = await _context.Bookings
                    .Include(b => b.UserProfile)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == request.BookingId && 
                                             b.UserProfile.IdentityUserId == userId);

                if (booking == null)
                {
                    return BadRequest(new { error = "Booking not found" });
                }

                // Verify payment with Stripe
                var isValid = await _paymentService.ValidatePaymentAsync(request.PaymentIntentId);
                if (!isValid)
                {
                    return BadRequest(new { error = "Payment validation failed" });
                }

                var paymentIntent = await _paymentService.GetPaymentIntentAsync(request.PaymentIntentId);

                // Update booking status
                booking.Status = "Confirmed";
                booking.PaymentIntentId = request.PaymentIntentId; // Save the ID

                // Update room availability
                if (booking.Room != null)
                {
                    booking.Room.IsAvailable = false;
                    booking.Room.Status = "Booked";
                }

                // Create payment record
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = paymentIntent.Amount / 100m, // Convert from cents
                    PaymentMethod = "card",
                    PaymentDate = DateTime.UtcNow,
                    PaymentStatus = "succeeded",
                    TransactionRefrence = paymentIntent.Id
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    bookingId = booking.BookingId,
                    message = "Payment successful! Your booking is confirmed."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: BookNow/Success/{bookingId} - Success page
        [HttpGet]
        public async Task<IActionResult> Success(int bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.UserProfile)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && 
                                         b.UserProfile.IdentityUserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found";
                return RedirectToAction("Index", "Home");
            }

            // Fallback confirmation if needed
            if (booking.Status.ToLower() == "pending" && !string.IsNullOrEmpty(booking.PaymentIntentId))
            {
                var isValid = await _paymentService.ValidatePaymentAsync(booking.PaymentIntentId);
                if (isValid)
                {
                    var paymentIntent = await _paymentService.GetPaymentIntentAsync(booking.PaymentIntentId);
                    
                    booking.Status = "Confirmed";
                    if (booking.Room != null)
                    {
                        booking.Room.IsAvailable = false;
                        booking.Room.Status = "Booked";
                    }

                    if (!booking.Payments.Any(p => p.TransactionRefrence == booking.PaymentIntentId))
                    {
                        var payment = new Payment
                        {
                            BookingId = booking.BookingId,
                            Amount = paymentIntent.Amount / 100m,
                            PaymentMethod = "card",
                            PaymentDate = DateTime.UtcNow,
                            PaymentStatus = "succeeded",
                            TransactionRefrence = paymentIntent.Id
                        };
                        _context.Payments.Add(payment);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return View(booking);
        }

        // GET: BookNow/BookingDetails/{id} - Booking details
        [HttpGet]
        public async Task<IActionResult> BookingDetails(int id)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.UserProfile)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == id && 
                                         b.UserProfile.IdentityUserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found";
                return RedirectToAction("Index", "Home");
            }

            return View(booking);
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}