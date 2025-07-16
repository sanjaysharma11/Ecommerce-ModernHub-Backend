using SendGrid;
using SendGrid.Helpers.Mail;

namespace ECommerceApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(string toEmail, string subject, string message, bool isHtml)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_config["SendGrid:FromEmail"], "E-Commerce App");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
            await client.SendEmailAsync(msg);
        }
    }
}
