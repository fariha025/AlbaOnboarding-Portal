using AlbaOnboarding.Data;
using AlbaOnboarding.Models;
using AlbaOnboarding.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AlbaOnboarding.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly EmailService _emailService;

        public AdminController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext db, EmailService emailService)
        {
            _userManager = userManager;
            _db = db;
            _emailService = emailService;
        }

        // Main dashboard
        public async Task<IActionResult> Index()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var hrs = await _userManager.GetUsersInRoleAsync("HR");
            var totalItems = await _db.ChecklistItems.CountAsync();

            var employeeData = new List<dynamic>();
            foreach (var emp in employees)
            {
                var approved = await _db.ChecklistSubmissions
                    .CountAsync(s => s.EmployeeId == emp.Id
                        && s.Status == SubmissionStatus.Approved);
                var pct = totalItems > 0 ? (approved * 100) / totalItems : 0;
                var assignedHR = hrs.FirstOrDefault(h => h.Id == emp.AssignedHRId);
                employeeData.Add(new
                {
                    Employee = emp,
                    Percentage = pct,
                    AssignedHRName = assignedHR?.FullName ?? "Not Assigned"
                });
            }

            ViewBag.EmployeeData = employeeData;
            ViewBag.HRList = hrs;
            ViewBag.TotalEmployees = employees.Count;
            ViewBag.Completed = employees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.Completed);
            ViewBag.InProgress = employees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.InProgress);
            ViewBag.NotStarted = employees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.NotStarted);
            ViewBag.TotalHR = hrs.Count;

            return View();
        }

        // Manage Users page
        public async Task<IActionResult> ManageUsers()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var hrs = await _userManager.GetUsersInRoleAsync("HR");
            ViewBag.Employees = employees;
            ViewBag.HRs = hrs;
            return View();
        }

        // Assign HR to employee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignHR(string employeeId, string hrId)
        {
            var employee = await _userManager.FindByIdAsync(employeeId);
            if (employee == null) return NotFound();

            employee.AssignedHRId = hrId;
            await _userManager.UpdateAsync(employee);

            TempData["Success"] = $"HR assigned to {employee.FullName} successfully.";
            return RedirectToAction("Index");
        }

        // Deactivate a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"{user.FullName} has been deactivated.";
            return RedirectToAction("ManageUsers");
        }

        // Reactivate a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"{user.FullName} has been reactivated.";
            return RedirectToAction("ManageUsers");
        }

        // Reports page
        public async Task<IActionResult> Reports()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var totalItems = await _db.ChecklistItems.CountAsync();
            var allSubmissions = await _db.ChecklistSubmissions.ToListAsync();

            // Department breakdown
            var deptGroups = employees
                .GroupBy(e => string.IsNullOrEmpty(e.Department) ? "Unassigned" : e.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(e => e.OnboardingStatus == OnboardingStatus.Completed),
                    InProgress = g.Count(e => e.OnboardingStatus == OnboardingStatus.InProgress),
                    NotStarted = g.Count(e => e.OnboardingStatus == OnboardingStatus.NotStarted)
                }).ToList();

            // Documents pending approval
            var pendingDocs = await _db.ChecklistSubmissions
                .Where(s => s.Status == SubmissionStatus.Uploaded)
                .CountAsync();

            ViewBag.DeptGroups = deptGroups;
            ViewBag.TotalEmployees = employees.Count;
            ViewBag.Completed = employees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.Completed);
            ViewBag.InProgress = employees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.InProgress);
            ViewBag.NotStarted = employees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.NotStarted);
            ViewBag.PendingDocs = pendingDocs;
            ViewBag.TotalDocs = allSubmissions.Count;
            ViewBag.ApprovedDocs = allSubmissions.Count(s =>
                s.Status == SubmissionStatus.Approved);

            return View();
        }

        // Export CSV report
        public async Task<IActionResult> ExportCsv()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            var hrs = await _userManager.GetUsersInRoleAsync("HR");
            var totalItems = await _db.ChecklistItems.CountAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Full Name,Email,Department,Job Title,Start Date," +
                "Assigned HR,Onboarding Status,Completion %");

            foreach (var emp in employees)
            {
                var approved = await _db.ChecklistSubmissions
                    .CountAsync(s => s.EmployeeId == emp.Id
                        && s.Status == SubmissionStatus.Approved);
                var pct = totalItems > 0 ? (approved * 100) / totalItems : 0;
                var hrName = hrs.FirstOrDefault(h => h.Id == emp.AssignedHRId)?.FullName
                    ?? "Not Assigned";

                sb.AppendLine($"{emp.FullName},{emp.Email},{emp.Department}," +
                    $"{emp.JobTitle},{emp.StartDate:yyyy-MM-dd}," +
                    $"{hrName},{emp.OnboardingStatus},{pct}%");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "ALBA_Onboarding_Report.csv");
        }

        // Invite user
        [HttpGet]
        public IActionResult InviteUser() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteUser(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(role))
            {
                TempData["Error"] = "Email and role are required.";
                return View();
            }

            // Check if user already exists
            if (await _userManager.FindByEmailAsync(email) != null)
            {
                TempData["Error"] = "A user with this email already exists.";
                return View();
            }

            // Check if invite already sent
            var existingToken = _db.InvitationTokens
                .Any(t => t.Email == email && !t.IsUsed && t.ExpiryDate > DateTime.Now);
            if (existingToken)
            {
                TempData["Error"] = "An active invitation already exists for this email.";
                return View();
            }
            var token = new InvitationToken
            {
                Email = email,
                Role = role,
                Token = Guid.NewGuid().ToString(),
                ExpiryDate = DateTime.Now.AddDays(7)
            };

            _db.InvitationTokens.Add(token);
            await _db.SaveChangesAsync();

            var link = $"{Request.Scheme}://{Request.Host}/Identity/Account/Register?token={token.Token}&email={Uri.EscapeDataString(email)}";

            await _emailService.SendEmailAsync(email,
                "You're invited to join ALBA Onboarding",
                $"<p>Hello,</p><p>You have been invited to the ALBA onboarding portal " +
                $"as <b>{role}</b>.</p><p><a href='{link}'>Click here to register</a></p>" +
                $"<p>This link expires in 7 days.</p>");

            TempData["Success"] = $"Invitation sent to {email}";
            return RedirectToAction("Index");
        }
    }
}