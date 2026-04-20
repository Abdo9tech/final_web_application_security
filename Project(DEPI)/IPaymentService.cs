using Stripe;

namespace Project_DEPI.Services
{
    public interface IPaymentService
    {
        Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency, string? description = null, Dictionary<string, string>? metadata = null);
        Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
        Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId);
        Task<bool> ValidatePaymentAsync(string paymentIntentId);
        string GetPublishableKey();
    }
}



