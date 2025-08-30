using JustAskIndia.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using JustAskIndia.Interfaces;
using JustAskIndia.Services;
namespace JustAskIndia.Pages
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _config;

        public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailSender emailSender, IConfiguration config)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _config = config;
        }

        [BindProperty]
        public string Email { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Page("/Account/ResetPassword", null,
                new { email = user.Email, token = token }, Request.Scheme);

            var htmlContent = $@"
            <p>Hello {user.UserName},</p>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}'>Reset Password</a>";

            await _emailSender.SendToSingle(user.Email, "Reset your password", htmlContent);
            TempData["Message"] = "Check your email for the password reset link.";
            return RedirectToPage("/Login");
        }
    }

}
