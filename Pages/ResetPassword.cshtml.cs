using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JustAskIndia.ViewModels; 
using JustAskIndia.Models; 

namespace JustAskIndia.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;

        public ResetPasswordModel(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public ResetPasswordVM ViewModel { get; set; }

        public void OnGet(string email, string token)
        {
            ViewModel = new ResetPasswordVM
            {
                Email = email,
                Token = token
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(ViewModel.Email);
            if (user == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToPage("/Account/Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, ViewModel.Token, ViewModel.Password);

            if (result.Succeeded)
            {
                TempData["Message"] = "Password reset successful.";
                return RedirectToPage("/Account/Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return Page();
        }
    }
}
