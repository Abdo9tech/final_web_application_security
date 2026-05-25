using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project_DEPI.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpServer = _configuration["SmtpSettings:Server"] ?? "smtp.gmail.com";
            var smtpPortStr = _configuration["SmtpSettings:Port"] ?? "587";
            var smtpSenderEmail = _configuration["SmtpSettings:SenderEmail"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            var smtpSenderName = _configuration["SmtpSettings:SenderName"] ?? "Bookify Hotel";

            if (string.IsNullOrEmpty(smtpSenderEmail) || smtpSenderEmail.StartsWith("YOUR_") ||
                string.IsNullOrEmpty(smtpPassword) || smtpPassword.StartsWith("YOUR_"))
            {
                _logger.LogWarning($"[DEVELOPMENT MOCK EMAIL] SMTP Credentials missing. To: {email} | Subject: {subject}");
                return;
            }

            try
            {
                int smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;
                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpSenderEmail, smtpPassword);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpSenderEmail, smtpSenderName),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[SMTP Error] Failed to send email to {email}: {ex.Message}");
                // We do not re-throw here to prevent crashing the PriceDropService background loop or other flows
            }
        }
    }
}
