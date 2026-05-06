using AlbaOnboarding.Models;
using AlbaOnboarding.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AlbaOnboarding.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager,
            EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var link = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", token = encodedToken, email = Input.Email },
                protocol: Request.Scheme);

            await _emailService.SendEmailAsync(
                Input.Email,
                "Reset Your ALBA Onboarding Password",
                $"<p>Hi {user.FullName},</p>" +
                $"<p>Click the link below to reset your password:</p>" +
                $"<p><a href='{link}'>Reset Password</a></p>" +
                $"<p>If you did not request this, ignore this email.</p>" +
                $"<p>ALBA Onboarding Portal</p>");

            return RedirectToPage("./ForgotPasswordConfirmation");
        }
    }
}
