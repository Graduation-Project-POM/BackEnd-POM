using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MIO.API.Settings;
using MIO.Core.Interfaces;



namespace MIO.Infrastructure.Services
{
    public class EmailService:IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _settings;
        

        public EmailService(ILogger<EmailService> logger, EmailSettings emailSettings)
        {
            _logger = logger;
            _settings = emailSettings;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _settings.SenderName,
                    _settings.SenderEmail));

                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                message.Body = new BodyBuilder
                {
                    HtmlBody = body
                }.ToMessageBody();

                using var smtp = new SmtpClient();
                
                await smtp.ConnectAsync(
                    _settings.SmtpHost,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _settings.SenderEmail,
                    _settings.SenderPassword);

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email");
                return false;
            }
        }
        public string AcceptedInMIOSubject()
        {
            return "Accepted in PMO";
        }

        public string AcceptedInMIOBody(string StudentName)
        {
            return $"""
                <p>Dear <strong>{StudentName}</strong>,</p>
                <p>
                    We are pleased to inform you that you have been
                    accepted into the training program for the
                    <strong>Palestinian Mathematics Olympiad</strong>.
                </p>
                <p>Best regards,<br/>MIO Team</p>
                """;
        }

    }
    
}
