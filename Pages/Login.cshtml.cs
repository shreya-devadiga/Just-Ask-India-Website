using JustAskIndia.DTOs;
using JustAskIndia.Models;
using JustAskIndia.Services;
using JustAskIndia.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JustAskIndia.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly AuthService _authService;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public LoginModel(
            AuthService authService,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailSender emailSender)
        {
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [BindProperty]
        public string ForgotEmail { get; set; } = "";

        public string Message { get; set; }

        public class InputModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public IActionResult OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Username) || string.IsNullOrWhiteSpace(Input.Password))
            {
                Message = "Username and password are required.";
                return Page();
            }

            var loginDto = new LoginDTO(Input.Username, Input.Password);
            var (success, msg, _) = await _authService.LoginAsync(loginDto);

            if (!success)
            {
                Message = msg ?? "Login failed. Please try again.";
                return Page();
            }

            var user = await _userManager.FindByNameAsync(Input.Username);
            if (user == null)
            {
                Message = "User not found.";
                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToPage("/Upload");
            }

            return RedirectToPage("/Index");
        }

        // ✅ Forgot Password handler
        public async Task<IActionResult> OnPostForgotPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(ForgotEmail))
            {
                Message = "Please enter your registered email.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(ForgotEmail);
            if (user == null)
            {
                Message = "No account found with this email.";
                return Page();
            }

            //  Generate token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Page("/ResetPassword", null,
                new { userId = user.Id, token = token }, Request.Scheme);

            // Email content
            var subject = "JustAskIndia - Password Reset";
            var body = $"Hello {user.UserName},<br><br>Please click the link below to reset your password:<br><a href='{resetLink}'>Reset Password</a><br><br>Thanks,<br>JustAskIndia Team";

            //  Send email
            var emailSuccess = await _emailSender.SendToSingle(ForgotEmail, subject, body);
            Message = emailSuccess ? "Reset link sent to your email." : "Failed to send email. Try again.";

            return Page();
        }
    }
}
