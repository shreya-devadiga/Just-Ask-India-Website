using System.Net.Mail;
using JustAskIndia.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace JustAskIndia.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SendGridEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendToSingle(string toAddress, string subject, string content)
        {
            var apiKey = _configuration["SendGrid:Key"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("shreya@noaxs.ai", "JustAskIndia");
            var to = new EmailAddress(toAddress);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", content);
            var response = await client.SendEmailAsync(msg);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> SendToMultiple(List<string> toAddresses, string? ccAddress, string subject, string content)
        {
            var apiKey = _configuration["SendGrid:Key"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("shreya@noaxs.ai", "JustAskIndia");

            List<EmailAddress> to = toAddresses.Select(email => new EmailAddress(email)).ToList();
            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, to, subject, "", content, false);
            var response = await client.SendEmailAsync(msg);
            return response.IsSuccessStatusCode;
        }
    }
}
