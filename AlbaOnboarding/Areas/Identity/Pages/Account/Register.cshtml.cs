using AlbaOnboarding.Data;
using AlbaOnboarding.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AlbaOnboarding.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _db;

        public RegisterModel(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string InvitationToken { get; set; }
        public string InvitedEmail { get; set; }
        public string InvitedRole { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 6)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Passwords do not match.")]
            [Display(Name = "Confirm Password")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet(string token = null, string email = null)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid invitation link.";
                return RedirectToPage("./Login");
            }

            // Validate token
            var invitation = _db.InvitationTokens
                .FirstOrDefault(t => t.Token == token
                    && t.Email == email
                    && !t.IsUsed
                    && t.ExpiryDate > DateTime.Now);

            if (invitation == null)
            {
                TempData["Error"] = "This invitation link is invalid or has expired.";
                return RedirectToPage("./Login");
            }

            InvitationToken = token;
            InvitedEmail = email;
            InvitedRole = invitation.Role;

            Input = new InputModel { Email = email };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string token, string role)
        {
            if (!ModelState.IsValid)
                return Page();

            // Validate token again
            var invitation = _db.InvitationTokens
                .FirstOrDefault(t => t.Token == token
                    && t.Email == Input.Email
                    && !t.IsUsed
                    && t.ExpiryDate > DateTime.Now);

            if (invitation == null)
            {
                ModelState.AddModelError("", "Invalid or expired invitation.");
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = Input.FullName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, invitation.Role);
                invitation.IsUsed = true;
                await _db.SaveChangesAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);

                if (invitation.Role == "HR")
                    return RedirectToAction("Dashboard", "HR");
                if (invitation.Role == "Employee")
                    return RedirectToAction("Dashboard", "Employee");

                return RedirectToPage("/Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return Page();
        }
    }
}
