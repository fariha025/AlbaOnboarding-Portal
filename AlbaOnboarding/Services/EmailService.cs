using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AlbaOnboarding.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config,
            ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail,
            string subject, string body)
        {
            // Fire and forget — do not await
            _ = Task.Run(async () =>
            {
                try
                {
                    var fromEmail = _config["GmailSettings:From"];
                    var password = _config["GmailSettings:Password"];
                    var displayName = _config["GmailSettings:DisplayName"]
                        ?? "ALBA Onboarding";

                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress(
                        displayName, fromEmail));
                    email.To.Add(new MailboxAddress("", toEmail));
                    email.Subject = subject;
                    email.Body = new TextPart("html") { Text = body };

                    using var smtp = new SmtpClient();
                    await smtp.ConnectAsync("smtp-relay.brevo.com",
                        587, SecureSocketOptions.StartTls);
                    await smtp.AuthenticateAsync(fromEmail, password);
                    await smtp.SendAsync(email);
                    await smtp.DisconnectAsync(true);

                    _logger.LogInformation(
                        $"Email sent to {toEmail}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Email failed to {toEmail}: {ex.Message}");
                    try
                    {
                        var logPath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "EmailLog.txt");
                        await File.AppendAllTextAsync(logPath,
                            $"\n[{DateTime.Now}] FAILED: " +
                            $"{ex.Message}\nTO: {toEmail}\n");
                    }
                    catch { }
                }
            });
        }
    }
}