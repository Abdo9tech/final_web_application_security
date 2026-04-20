using Stripe;
using Microsoft.Extensions.Configuration;

namespace Project_DEPI.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly string _secretKey;
        private readonly string? _publishableKey;
        private readonly PaymentIntentService _paymentIntentService;
        private readonly RefundService _refundService;

        public StripePaymentService(IConfiguration configuration)
        {
            _secretKey = configuration["Stripe:SecretKey"] ?? throw new ArgumentException("Stripe SecretKey is not configured");
            _publishableKey = configuration["Stripe:PublishableKey"];
            
            StripeConfiguration.ApiKey = _secretKey;
            _paymentIntentService = new PaymentIntentService();
            _refundService = new RefundService();
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency, string? description = null, Dictionary<string, string>? metadata = null)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency.ToLower(),
                Description = description,
                Metadata = metadata,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                CaptureMethod = "automatic"
            };

            return await _paymentIntentService.CreateAsync(options);
        }

        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            return await _paymentIntentService.GetAsync(paymentIntentId);
        }

        public async Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId)
        {
            var options = new PaymentIntentConfirmOptions();
            return await _paymentIntentService.ConfirmAsync(paymentIntentId, options);
        }

        public async Task<bool> ValidatePaymentAsync(string paymentIntentId)
        {
            try
            {
                var paymentIntent = await GetPaymentIntentAsync(paymentIntentId);
                return paymentIntent.Status == "succeeded";
            }
            catch
            {
                return false;
            }
        }

        public string GetPublishableKey()
        {
            return _publishableKey ?? "";
        }

        public async Task<Refund> CreateRefundAsync(string paymentIntentId, decimal? amount = null)
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amount.HasValue ? (long)(amount.Value * 100) : null
            };

            return await _refundService.CreateAsync(options);
        }
    }
}