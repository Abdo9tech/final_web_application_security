using Stripe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Project_DEPI.Services;
using Project_DEPI.ViewModels;
using BookifyHotel.Data;
using BookifyHotel.Model;
using DAL.Constants;
using BookingService = PLL.Services.BookingService;
using RoomService = PLL.Services.RoomService;

namespace Project_DEPI.Controllers
{
    [Authorize]
    public class BookNowController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly BookifyHotelDbContext _context;
        private readonly RoomService _roomService;
        private readonly BookingService _bookingService;
        private readonly ILogger<BookNowController> _logger;

        public BookNowController(
            IPaymentService paymentService,
            BookifyHotelDbContext context,
            RoomService roomService,
            BookingService bookingService,
            ILogger<BookNowController> logger)
        {
            _paymentService = paymentService;
            _context = context;
            _roomService = roomService;
            _bookingService = bookingService;
            _logger = logger;
        }

        // GET: BookNow/Index/{id} or BookNow/Index?roomId={id}
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

        // GET: BookNow/Room/{id}
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

        // POST: BookNow/ProcessBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBooking(BookNowViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    TempData["Error"] = "Please fill in all required fields. " + string.Join(", ", errors);
                    return View("Index", model);
                }

                var userId = GetCurrentUserId();

                // Get user profile
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.IdentityUserId == userId);

                if (userProfile == null)
                {
                    TempData["Error"] = "User profile not found. Please complete your profile first.";
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    return View("Index", model);
                }

                // Re-fetch room from DB (never trust client-submitted price)
                var room = await _context.Rooms
                    .Include(r => r.RoomType)
                    .FirstOrDefaultAsync(r => r.RoomId == model.RoomId);

                if (room == null || !room.IsAvailable)
                {
                    TempData["Error"] = "Selected room is no longer available.";
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    return View("Index", model);
                }

                // Validate dates
                if (model.CheckInDate < DateTime.Today)
                {
                    ModelState.AddModelError("CheckInDate", "Check-in date cannot be in the past.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    return View("Index", model);
                }

                if (model.CheckOutDate <= model.CheckInDate)
                {
                    ModelState.AddModelError("CheckOutDate", "Check-out date must be after check-in date.");
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    return View("Index", model);
                }

                // Check for date conflicts - only against Confirmed bookings
                var hasConflict = await _context.Bookings
                    .AnyAsync(b => b.RoomId == model.RoomId &&
                                  b.Status == "Confirmed" &&
                                  ((model.CheckInDate >= b.CheckInDate && model.CheckInDate < b.CheckOutDate) ||
                                   (model.CheckOutDate > b.CheckInDate && model.CheckOutDate <= b.CheckOutDate) ||
                                   (model.CheckInDate <= b.CheckInDate && model.CheckOutDate >= b.CheckOutDate)));

                if (hasConflict)
                {
                    TempData["Error"] = "Room is not available for the selected dates.";
                    model.PublishableKey = _paymentService.GetPublishableKey();
                    return View("Index", model);
                }

                // SERVER-SIDE price calculation — never trust client
                var pricePerNight = room.RoomType?.PricePerNight ?? 0;
                var nights = Math.Max(1, (model.CheckOutDate - model.CheckInDate).Days);
                var baseTotal = pricePerNight * nights;
                var taxAmount = Math.Round(baseTotal * 0.10m, 2); // 10% tax
                var grandTotal = baseTotal + taxAmount;

                // Create booking with Pending status
                var booking = new Booking
                {
                    UserProfileId = userProfile.Id,
                    RoomId = model.RoomId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    NumberOfGuests = model.NumberOfGuests > 0 ? model.NumberOfGuests : 1,
                    TotalPrice = grandTotal,
                    Status = BookingStatus.Pending,
                    BookingDate = DateTime.Now
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Create Stripe PaymentIntent
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

                booking.PaymentIntentId = paymentIntent.Id;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Booking {BookingId} created for user {UserId}, PaymentIntent {PaymentIntentId}",
                    booking.BookingId, userId, paymentIntent.Id);

                return RedirectToAction("Payment", new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing booking for user {UserId}", GetCurrentUserId());
                TempData["Error"] = $"An error occurred while processing your booking: {ex.Message}";
                model.PublishableKey = _paymentService.GetPublishableKey();
                return View("Index", model);
            }
        }

        // GET: BookNow/Payment/{bookingId}
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

            // Allow access only for Pending bookings
            if (!BookingStatus.IsPending(booking.Status))
            {
                if (BookingStatus.IsConfirmed(booking.Status))
                    return RedirectToAction("Success", new { bookingId });

                TempData["Error"] = "This booking cannot be paid";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            // Retrieve or recreate PaymentIntent
            PaymentIntent paymentIntent;
            try
            {
                if (!string.IsNullOrEmpty(booking.PaymentIntentId))
                {
                    paymentIntent = await _paymentService.GetPaymentIntentAsync(booking.PaymentIntentId);

                    // If already succeeded, confirm the booking and redirect to success
                    if (paymentIntent.Status == "succeeded")
                    {
                        await ConfirmBookingInternally(booking, paymentIntent);
                        return RedirectToAction("Success", new { bookingId });
                    }

                    // If cancelled/expired, create a new one
                    if (paymentIntent.Status == "canceled")
                    {
                        paymentIntent = await CreateNewPaymentIntent(booking);
                    }
                }
                else
                {
                    paymentIntent = await CreateNewPaymentIntent(booking);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PaymentIntent retrieval failed for booking {BookingId}, creating new one", bookingId);
                paymentIntent = await CreateNewPaymentIntent(booking);
            }

            var viewModel = new PaymentViewModel
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalPrice,
                RoomNumber = booking.Room.RoomNumber.ToString(),
                RoomType = booking.Room.RoomType?.Name ?? "Standard",
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                NumberOfNights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days),
                PricePerNight = booking.Room.RoomType?.PricePerNight ?? 0,
                PublishableKey = _paymentService.GetPublishableKey(),
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret
            };

            return View(viewModel);
        }

        // POST: BookNow/ConfirmPayment - Called by Stripe JS after payment
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var booking = await _context.Bookings
                    .Include(b => b.UserProfile)
                    .Include(b => b.Room)
                    .Include(b => b.Payments)
                    .FirstOrDefaultAsync(b => b.BookingId == request.BookingId &&
                                             b.UserProfile.IdentityUserId == userId);

                if (booking == null)
                {
                    _logger.LogWarning("ConfirmPayment: booking {BookingId} not found for user {UserId}",
                        request.BookingId, userId);
                    return BadRequest(new { error = "Booking not found" });
                }

                // Already confirmed — idempotent response
                if (BookingStatus.IsConfirmed(booking.Status))
                {
                    return Json(new { success = true, bookingId = booking.BookingId, message = "Booking already confirmed." });
                }

                // Validate payment with Stripe
                var isValid = await _paymentService.ValidatePaymentAsync(request.PaymentIntentId);
                if (!isValid)
                {
                    _logger.LogWarning("Payment validation failed for PaymentIntent {PaymentIntentId}, booking {BookingId}",
                        request.PaymentIntentId, request.BookingId);
                    return BadRequest(new { error = "Payment validation failed. Please try again." });
                }

                var paymentIntent = await _paymentService.GetPaymentIntentAsync(request.PaymentIntentId);
                await ConfirmBookingInternally(booking, paymentIntent);

                _logger.LogInformation("Booking {BookingId} confirmed via ConfirmPayment for user {UserId}",
                    booking.BookingId, userId);

                return Json(new
                {
                    success = true,
                    bookingId = booking.BookingId,
                    message = "Payment successful! Your booking is confirmed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for booking {BookingId}", request?.BookingId);
                return BadRequest(new { error = "An error occurred confirming your payment. Please contact support." });
            }
        }

        // GET: BookNow/Success/{bookingId} - Stripe redirects here after payment
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

            // Auto-confirm if Stripe already processed the payment (redirect flow)
            if (BookingStatus.IsPending(booking.Status) && !string.IsNullOrEmpty(booking.PaymentIntentId))
            {
                try
                {
                    var isValid = await _paymentService.ValidatePaymentAsync(booking.PaymentIntentId);
                    if (isValid)
                    {
                        var paymentIntent = await _paymentService.GetPaymentIntentAsync(booking.PaymentIntentId);
                        await ConfirmBookingInternally(booking, paymentIntent);

                        _logger.LogInformation("Booking {BookingId} auto-confirmed on Success page for user {UserId}",
                            bookingId, userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto-confirm failed for booking {BookingId} on Success page", bookingId);
                }
            }

            return View(booking);
        }

        // GET: BookNow/BookingDetails/{id}
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

        // POST: BookNow/CancelBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.UserProfile)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                         b.UserProfile.IdentityUserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found";
                return RedirectToAction("MyBookings", "Booking");
            }

            if (BookingStatus.IsCompleted(booking.Status) || BookingStatus.IsCancelled(booking.Status))
            {
                TempData["Error"] = "This booking cannot be cancelled";
                return RedirectToAction("BookingDetails", new { id = bookingId });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.Now;

            // Release the room
            if (booking.Room != null)
            {
                var hasOtherActiveBookings = await _context.Bookings
                    .AnyAsync(b => b.RoomId == booking.RoomId &&
                                   b.BookingId != bookingId &&
                                   b.Status == "Confirmed" &&
                                   b.CheckInDate <= DateTime.Now &&
                                   b.CheckOutDate >= DateTime.Now);

                if (!hasOtherActiveBookings)
                {
                    booking.Room.IsAvailable = true;
                    booking.Room.Status = RoomStatus.Available;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled successfully.";
            return RedirectToAction("MyBookings", "Booking");
        }

        // ─── Private Helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Confirms a booking and updates room status. Idempotent.
        /// </summary>
        private async Task ConfirmBookingInternally(Booking booking, PaymentIntent paymentIntent)
        {
            // Idempotency guard
            if (BookingStatus.IsConfirmed(booking.Status)) return;

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.Now;
            booking.PaymentIntentId = paymentIntent.Id;

            // Mark room as booked
            if (booking.Room != null)
            {
                booking.Room.IsAvailable = false;
                booking.Room.Status = RoomStatus.Booked;
            }

            // Create payment record only if not already recorded
            var alreadyRecorded = await _context.Payments
                .AnyAsync(p => p.PaymentIntentId == paymentIntent.Id);

            if (!alreadyRecorded)
            {
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = paymentIntent.Amount / 100m, // Stripe stores in cents
                    PaymentMethod = "card",
                    PaymentDate = DateTime.UtcNow,
                    PaymentStatus = "succeeded",
                    PaymentIntentId = paymentIntent.Id,
                    TransactionRefrence = paymentIntent.Id,
                    Currency = paymentIntent.Currency ?? "usd"
                };
                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a new PaymentIntent for a booking and saves the ID.
        /// </summary>
        private async Task<PaymentIntent> CreateNewPaymentIntent(Booking booking)
        {
            var paymentIntent = await _paymentService.CreatePaymentIntentAsync(
                booking.TotalPrice,
                "usd",
                $"Hotel Booking #{booking.BookingId}"
            );
            booking.PaymentIntentId = paymentIntent.Id;
            await _context.SaveChangesAsync();
            return paymentIntent;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}
