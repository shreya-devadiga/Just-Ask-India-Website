using JustAskIndia.DTOs;
using JustAskIndia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace JustAskIndia.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AuthService _authService;

        public RegisterModel(AuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public RegisterDTO Input { get; set; } = new RegisterDTO("", "", "", "", "", "", "");

        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                IsSuccess = false;
                Message = "Invalid form submission.";
                return Page();
            }

            var (success, responseMessage) = await _authService.RegisterAsync(Input);

            if (success)
            {
                TempData["SuccessMessage"] = "Registered successfully!";
                return RedirectToPage("Login"); // Redirect to login
            }

            IsSuccess = false;
            Message = responseMessage;
            return Page();
        }
    }
}
