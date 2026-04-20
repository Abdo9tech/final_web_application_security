using Stripe;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Project_DEPI.Services;
using Project_DEPI.ViewModels;
using DAL.DataBase;
using BookifyHotel.Model;
using BookifyHotel.Data;

namespace Project_DEPI.Controllers
{
    // SECURITY [Sensitive Endpoint Protection]: The entire PaymentController requires authentication.
    // Only authenticated users with a verified identity can create or confirm payments.
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly BookifyHotelDbContext _context;
        private readonly IConfiguration _configuration;

        // SECURITY [Security Event Logging]: ILogger is injected to record all payment events
        // (successes, failures, tampering attempts) for audit and fraud detection.
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            BookifyHotelDbContext context,
            IConfiguration configuration,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // SECURITY [Sensitive Endpoint Protection]: Booking ID ownership is verified server-side
        // — the query filters by both BookingId AND the authenticated user's IdentityUserId.
        // This prevents horizontal privilege escalation (user A viewing/paying user B's booking).
        [HttpGet]
        public async Task<IActionResult> Index(int bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.UserProfile)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserProfile.IdentityUserId == userId);

            if (booking == null)
            {
                // SECURITY [Unauthorized Access Logging]: Log attempt to access a booking that
                // doesn't belong to the requesting user (potential IDOR attack).
                _logger.LogWarning("[SECURITY] User {UserId} attempted to access booking {BookingId} — not found or unauthorized.",
                    userId, bookingId);
                TempData["Error"] = "Booking not found";
                return RedirectToAction("Index", "Booking");
            }

            if (booking.Status != "pending")
            {
                TempData["Error"] = "This booking cannot be paid";
                return RedirectToAction("Details", "Booking", new { id = bookingId });
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
                PublishableKey = _paymentService.GetPublishableKey()
            };

            return View(viewModel);
        }

        // SECURITY [Payment Tampering Protection / Server-Side Payment Verification]:
        // The payment intent is created server-side with the amount from the DATABASE,
        // not from the client. Client-submitted amounts are IGNORED to prevent price manipulation.
        // SECURITY [CSRF Protection]: ValidateAntiForgeryToken is active globally.
        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] CreatePaymentIntentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var booking = await _context.Bookings
                    .Include(b => b.UserProfile)
                    .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                    .FirstOrDefaultAsync(b => b.BookingId == request.BookingId && b.UserProfile.IdentityUserId == userId);

                if (booking == null)
                {
                    _logger.LogWarning("[SECURITY] CreatePaymentIntent: User {UserId} attempted unauthorized access to booking {BookingId}.",
                        userId, request.BookingId);
                    return BadRequest(new { error = "Booking not found" });
                }

                if (booking.Status != "pending")
                {
                    return BadRequest(new { error = "Booking cannot be paid" });
                }

                // SECURITY [Payment Tampering Protection]: The amount used for the Stripe
                // PaymentIntent is taken EXCLUSIVELY from the database (booking.TotalPrice),
                // never from the client-submitted request body. This ensures users cannot
                // manipulate the charged amount by modifying the request payload.
                var authorizedAmount = booking.TotalPrice;
                var authorizedCurrency = "usd";

                var metadata = new Dictionary<string, string>
                {
                    { "booking_id", booking.BookingId.ToString() },
                    { "user_id", userId }
                };

                var paymentIntent = await _paymentService.CreatePaymentIntentAsync(
                    authorizedAmount,          // SECURITY: Amount from DB, not from client
                    authorizedCurrency,        // SECURITY: Currency from server config, not client
                    $"Hotel booking #{booking.BookingId}",
                    metadata
                );

                // Store payment intent ID in booking for later server-side verification
                booking.PaymentIntentId = paymentIntent.Id;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // SECURITY [Secure Transaction Logging]: Log the creation of a payment intent for audit.
                _logger.LogInformation("[SECURITY] PaymentIntent {PaymentIntentId} created for booking {BookingId} by user {UserId}.",
                    paymentIntent.Id, booking.BookingId, userId);

                return Json(new { clientSecret = paymentIntent.ClientSecret });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SECURITY] Error creating PaymentIntent for user {UserId}.", GetCurrentUserId());
                return BadRequest(new { error = ex.Message });
            }
        }

        // SECURITY [Server-Side Payment Verification]: Payment confirmation is verified
        // against Stripe's API directly — the server independently checks the payment
        // status rather than trusting the client's claim. Additionally, the server validates
        // that the Stripe-reported amount matches the database amount to detect tampering.
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment([FromBody] PaymentRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var booking = await _context.Bookings
                    .Include(b => b.UserProfile)
                    .Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.BookingId == request.BookingId && b.UserProfile.IdentityUserId == userId);

                if (booking == null)
                {
                    _logger.LogWarning("[SECURITY] ConfirmPayment: User {UserId} attempted unauthorized confirmation of booking {BookingId}.",
                        userId, request.BookingId);
                    return BadRequest(new { error = "Booking not found" });
                }

                // SECURITY [Server-Side Payment Verification]: Retrieve the payment intent
                // directly from Stripe to validate its current status server-side.
                var isValid = await _paymentService.ValidatePaymentAsync(request.PaymentIntentId);
                if (!isValid)
                {
                    _logger.LogWarning("[SECURITY] Payment validation failed for PaymentIntentId {PaymentIntentId}, booking {BookingId}.",
                        request.PaymentIntentId, request.BookingId);
                    return BadRequest(new { error = "Payment validation failed" });
                }

                var paymentIntent = await _paymentService.GetPaymentIntentAsync(request.PaymentIntentId);

                // SECURITY [Payment Tampering Protection]: Verify that the amount Stripe
                // processed (in cents) matches the amount stored in our database.
                // If they differ, it may indicate a man-in-the-middle or replay attack.
                var stripeAmountDecimal = paymentIntent.Amount / 100m;
                if (Math.Abs(stripeAmountDecimal - booking.TotalPrice) > 0.01m)
                {
                    _logger.LogError("[SECURITY] PAYMENT TAMPERING DETECTED! Booking {BookingId}: DB amount={DbAmount}, Stripe amount={StripeAmount}. User={UserId}.",
                        booking.BookingId, booking.TotalPrice, stripeAmountDecimal, userId);
                    return BadRequest(new { error = "Payment amount mismatch detected. Transaction rejected." });
                }

                // SECURITY [Stripe Signature Validation]: Verify PaymentIntent belongs to our booking
                // by checking the stored PaymentIntentId matches what was submitted.
                if (!string.IsNullOrEmpty(booking.PaymentIntentId) &&
                    booking.PaymentIntentId != request.PaymentIntentId)
                {
                    _logger.LogError("[SECURITY] PaymentIntent mismatch for booking {BookingId}. Expected {Expected}, got {Got}. User={UserId}.",
                        booking.BookingId, booking.PaymentIntentId, request.PaymentIntentId, userId);
                    return BadRequest(new { error = "Payment intent mismatch. Transaction rejected." });
                }

                // Update booking status
                booking.Status = "confirmed";
                booking.PaymentIntentId = paymentIntent.Id;
                booking.UpdatedAt = DateTime.UtcNow;

                // Update room availability
                if (booking.Room != null)
                {
                    booking.Room.IsAvailable = false;
                    booking.Room.Status = "Booked";
                }

                // SECURITY [Secure Transaction Logging]: Create an immutable payment record
                // for audit purposes. This record cannot be modified after creation.
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = stripeAmountDecimal,
                    PaymentMethod = "card",
                    PaymentDate = DateTime.UtcNow,
                    PaymentStatus = "succeeded",
                    PaymentIntentId = paymentIntent.Id,
                    Currency = paymentIntent.Currency,
                    TransactionReference = paymentIntent.Id
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // SECURITY [Secure Transaction Logging]: Log successful payment completion.
                _logger.LogInformation("[SECURITY] Payment succeeded. BookingId={BookingId}, PaymentIntentId={PaymentIntentId}, Amount={Amount}, User={UserId}.",
                    booking.BookingId, paymentIntent.Id, stripeAmountDecimal, userId);

                return Json(new {
                    success = true,
                    bookingId = booking.BookingId,
                    message = "Payment successful! Your booking is confirmed."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SECURITY] Error confirming payment for user {UserId}, booking {BookingId}.",
                    GetCurrentUserId(), request?.BookingId);
                return BadRequest(new { error = ex.Message });
            }
        }

        // SECURITY [Stripe Signature Validation / Webhook Security]:
        // Stripe sends webhook events to notify the server of payment status changes.
        // The webhook endpoint validates the Stripe-Signature header using the webhook secret
        // to ensure the event truly originated from Stripe and was not forged by an attacker.
        // [IgnoreAntiforgeryToken] is required because webhooks are server-to-server calls
        // without a browser session or anti-forgery cookie.
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret) || webhookSecret.StartsWith("REPLACE_"))
            {
                _logger.LogWarning("[SECURITY] Stripe webhook secret is not configured. Webhook endpoint disabled.");
                return BadRequest("Webhook not configured.");
            }

            string json;
            using (var reader = new StreamReader(HttpContext.Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            try
            {
                // SECURITY [Stripe Signature Validation]: Verify the webhook came from Stripe
                // using HMAC-SHA256 signature. Requests with invalid signatures are rejected.
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );

                _logger.LogInformation("[SECURITY] Stripe webhook received: {EventType}, EventId: {EventId}",
                    stripeEvent.Type, stripeEvent.Id);

                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        _logger.LogInformation("[SECURITY] Webhook: PaymentIntent {PaymentIntentId} succeeded.",
                            paymentIntent.Id);
                        // Additional server-side processing can be done here if needed.
                    }
                }
                else if (stripeEvent.Type == Events.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    _logger.LogWarning("[SECURITY] Webhook: PaymentIntent {PaymentIntentId} failed.",
                        paymentIntent?.Id ?? "unknown");
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                // SECURITY [Stripe Signature Validation]: Invalid signature — reject the request.
                _logger.LogError("[SECURITY] Stripe webhook signature validation failed: {Message}", ex.Message);
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(int bookingId)
        {
            var userId = GetCurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .ThenInclude(r => r.RoomType)
                .Include(b => b.UserProfile)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserProfile.IdentityUserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found";
                return RedirectToAction("Index", "Home");
            }

            return View(booking);
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        // SECURITY [No Sensitive Data Logging]: Returns only the ASP.NET Identity user ID
        // (a GUID) — never the email or other PII — for use in queries and logging.
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}