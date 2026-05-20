using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Project_DEPI.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var server = _configuration["SmtpSettings:Server"] ?? "smtp.gmail.com";
            var portStr = _configuration["SmtpSettings:Port"] ?? "587";
            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            var password = _configuration["SmtpSettings:Password"];
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "Bookify Hotel";

            // Graceful fallback if credentials are not configured yet
            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password) || senderEmail.StartsWith("YOUR_"))
            {
                System.Console.WriteLine($"[DEVELOPMENT MOCK EMAIL] To: {email}\nSubject: {subject}\nMessage: {htmlMessage}\n");
                return;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;

            using (var client = new SmtpClient(server, port))
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
