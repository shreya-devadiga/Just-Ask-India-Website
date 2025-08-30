using MailKit.Net.Smtp;
using MimeKit;
using JustAskIndia.Interfaces;

namespace JustAskIndia.Services;

public class SmtpEmailSender : IEmailSender
{
    private const string SmtpServer = "mail.noaxs.ai";
    private const int SmtpPort = 465;
    private const string SenderEmail = "shreya@noaxs.ai";
    private const string SenderPassword = "noaxs@2025";
    private const string SenderName = "JustAskIndia - Notification";

    public async Task<bool> SendToMultiple(List<string> toAddresses, string? ccAddress, string subject, string content)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync("mail.noaxs.ai", 465, true);
            await client.AuthenticateAsync(SenderEmail, SenderPassword);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(SenderName, SenderEmail));

            foreach (var toAddress in toAddresses)
                message.To.Add(new MailboxAddress(toAddress, toAddress));

            if (!string.IsNullOrWhiteSpace(ccAddress))
                message.Cc.Add(new MailboxAddress(ccAddress, ccAddress));

            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = content
            };

            var result = await client.SendAsync(message);
            Console.WriteLine(result ?? "No-Response");

            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Email Error: " + ex.Message);
            return false;
        }
    }

    public async Task<bool> SendToSingle(string toAddress, string subject, string content)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpServer, SmtpPort, true);
            await client.AuthenticateAsync(SenderEmail, SenderPassword);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(SenderName, SenderEmail));
            message.To.Add(new MailboxAddress(toAddress, toAddress));

            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = content
            };

            var result = await client.SendAsync(message);
            Console.WriteLine(result ?? "No-Response");

            await client.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Email Error: " + ex.Message);
            return false;
        }
    }
}
