using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AlbaOnboarding.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = _config["GmailSettings:From"];
                var password = _config["GmailSettings:Password"];
                var displayName = _config["GmailSettings:DisplayName"];

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(displayName, fromEmail));
                email.To.Add(new MailboxAddress("", toEmail));
                email.Subject = subject;
                email.Body = new TextPart("html") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(fromEmail, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log to file if email fails so app doesn't crash
                var logPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "EmailLog.txt");
                await File.AppendAllTextAsync(logPath,
                    $"\n[{DateTime.Now}] EMAIL FAILED: {ex.Message}\n" +
                    $"TO: {toEmail}\nSUBJECT: {subject}\n");
            }
        }
    }
}