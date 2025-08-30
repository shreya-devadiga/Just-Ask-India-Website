using JustAskIndia.Data;
using JustAskIndia.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JustAskIndia.Interfaces;

namespace JustAskIndia.Services
{
    public class EmailService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;

        public EmailService(AppDbContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        //public async Task<(bool Success, string Message)> GenerateAndSendOtpAsync(string email)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        //    if (user == null)
        //        return (false, "User not found");

        //    string otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        //    var expiry = DateTime.UtcNow.AddMinutes(10);

        //    user.OtpCode = otp;
        //    user.OtpExpiry = expiry;
        //    await _context.SaveChangesAsync();

        //    string body = $"Hi {user.UserName},<br><br>Your OTP is <strong>{otp}</strong>. It will expire in 10 minutes.";
        //    await _emailSender.SendToSingle(user.Email, "Your OTP Code - JustAskIndia", body);

        //    return (true, "OTP sent successfully");
        //}

        public async Task<bool> SendPasswordResetLinkAsync(string email, string resetLink)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return false;

            string body = $"Hi {user.UserName},<br><br>Click <a href='{resetLink}'>here</a> to reset your password.";
            await _emailSender.SendToSingle(user.Email, "Password Reset - JustAskIndia", body);
            return true;
        }
    }
}
