using AlbaOnboarding.Data;
using AlbaOnboarding.Models;
using AlbaOnboarding.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlbaOnboarding.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly EmailService _emailService;

        public HRController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext db, EmailService emailService)
        {
            _userManager = userManager;
            _db = db;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentHR = await _userManager.GetUserAsync(User);
            var allEmployees = await _userManager.GetUsersInRoleAsync("Employee");
            var myEmployees = allEmployees
                .Where(e => e.AssignedHRId == currentHR.Id).ToList();

            var totalItems = await _db.ChecklistItems.CountAsync();
            var employeeData = new List<dynamic>();

            foreach (var emp in myEmployees)
            {
                var approved = await _db.ChecklistSubmissions
                    .CountAsync(s => s.EmployeeId == emp.Id
                        && s.Status == SubmissionStatus.Approved);
                var pct = totalItems > 0 ? (approved * 100) / totalItems : 0;
                employeeData.Add(new
                {
                    Employee = emp,
                    ApprovedCount = approved,
                    TotalItems = totalItems,
                    Percentage = pct
                });
            }

            ViewBag.EmployeeData = employeeData;
            ViewBag.TotalAssigned = myEmployees.Count;
            ViewBag.Completed = myEmployees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.Completed);
            ViewBag.InProgress = myEmployees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.InProgress);
            ViewBag.NotStarted = myEmployees.Count(e =>
                e.OnboardingStatus == OnboardingStatus.NotStarted);

            return View();
        }

        public async Task<IActionResult> ReviewEmployee(string employeeId)
        {
            var currentHR = await _userManager.GetUserAsync(User);
            var employee = await _userManager.FindByIdAsync(employeeId);

            if (employee == null || employee.AssignedHRId != currentHR.Id)
                return Forbid();

            var items = await _db.ChecklistItems
                .OrderBy(c => c.DisplayOrder).ToListAsync();
            var submissions = await _db.ChecklistSubmissions
                .Where(s => s.EmployeeId == employeeId).ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.Items = items;
            ViewBag.Submissions = submissions;
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> EditEmployeeDetails(string employeeId)
        {
            var currentHR = await _userManager.GetUserAsync(User);
            var employee = await _userManager.FindByIdAsync(employeeId);

            if (employee == null || employee.AssignedHRId != currentHR.Id)
                return Forbid();

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployeeDetails(
            string employeeId, string jobTitle, string grade,
            string badgeNumber, string prfNumber, string referenceNumber,
            string preparedBy, string reviewedBy, string startDate)
        {
            var currentHR = await _userManager.GetUserAsync(User);
            var employee = await _userManager.FindByIdAsync(employeeId);

            if (employee == null || employee.AssignedHRId != currentHR.Id)
                return Forbid();

            var errors = new List<string>();

            // Badge number validation — 4 to 6 digits
            if (!string.IsNullOrEmpty(badgeNumber) &&
                !System.Text.RegularExpressions.Regex
                    .IsMatch(badgeNumber, @"^\d{4,6}$"))
                errors.Add("Badge number must be 4 to 6 digits only.");

            // PRF number validation — format PRF-YYYY-NNN
            if (!string.IsNullOrEmpty(prfNumber) &&
                !System.Text.RegularExpressions.Regex
                    .IsMatch(prfNumber, @"^PRF-\d{4}-\d{2,4}$"))
                errors.Add("PRF number must follow format: PRF-2026-042");

            // Reference number validation — format REF-YYYY-XXXXX
            if (!string.IsNullOrEmpty(referenceNumber) &&
                !System.Text.RegularExpressions.Regex
                    .IsMatch(referenceNumber, @"^REF-\d{4}-[A-Z0-9]{3,5}$"))
                errors.Add("Reference number must follow format: REF-2026-A2B3");

            if (errors.Any())
            {
                TempData["Error"] = string.Join(" | ", errors);
                return View(employee);
            }

            if (!string.IsNullOrEmpty(jobTitle))
                employee.JobTitle = jobTitle;
            if (!string.IsNullOrEmpty(grade))
                employee.Grade = grade;
            if (!string.IsNullOrEmpty(badgeNumber))
                employee.BadgeNumber = badgeNumber;
            if (!string.IsNullOrEmpty(prfNumber))
                employee.PrfNumber = prfNumber;
            if (!string.IsNullOrEmpty(referenceNumber))
                employee.ReferenceNumber = referenceNumber;
            if (!string.IsNullOrEmpty(preparedBy))
                employee.PreparedBy = preparedBy;
            if (!string.IsNullOrEmpty(reviewedBy))
                employee.ReviewedByName = reviewedBy;
            if (!string.IsNullOrEmpty(startDate) &&
                DateTime.TryParse(startDate, out DateTime pd))
                employee.StartDate = pd;

            var result = await _userManager.UpdateAsync(employee);

            if (result.Succeeded)
                TempData["Success"] = "Employee details updated successfully.";
            else
                TempData["Error"] = "Update failed. Please try again.";

            return RedirectToAction("EditEmployeeDetails",
                new { employeeId = employeeId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDocument(int submissionId, string comment)
        {
            var submission = await _db.ChecklistSubmissions
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return NotFound();

            var currentHR = await _userManager.GetUserAsync(User);
            submission.Status = SubmissionStatus.Approved;
            submission.ReviewedAt = DateTime.Now;
            submission.ReviewedBy = currentHR.FullName;
            submission.ReviewComment = comment;
            await _db.SaveChangesAsync();

            await CheckAndUpdateOnboardingStatus(submission.EmployeeId);

            await _emailService.SendEmailAsync(
                submission.Employee.Email,
                "Document Approved - ALBA Onboarding",
                $"<p>Hi {submission.Employee.FullName},</p>" +
                $"<p>Your document has been <b>approved</b>.</p>" +
                $"<p>Comment: {comment}</p>");

            TempData["Success"] = "Document approved.";
            return RedirectToAction("ReviewEmployee",
                new { employeeId = submission.EmployeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDocument(int submissionId, string comment)
        {
            var submission = await _db.ChecklistSubmissions
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return NotFound();

            var currentHR = await _userManager.GetUserAsync(User);
            submission.Status = SubmissionStatus.Rejected;
            submission.ReviewedAt = DateTime.Now;
            submission.ReviewedBy = currentHR.FullName;
            submission.ReviewComment = comment;
            await _db.SaveChangesAsync();

            await _emailService.SendEmailAsync(
                submission.Employee.Email,
                "Document Rejected - ALBA Onboarding",
                $"<p>Hi {submission.Employee.FullName},</p>" +
                $"<p>Your document was <b>rejected</b>. Please re-upload.</p>" +
                $"<p>Reason: {comment}</p>");

            TempData["Error"] = "Document rejected.";
            return RedirectToAction("ReviewEmployee",
                new { employeeId = submission.EmployeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOnboarding(string employeeId)
        {
            var employee = await _userManager.FindByIdAsync(employeeId);
            if (employee == null) return NotFound();

            employee.OnboardingStatus = OnboardingStatus.Completed;
            await _userManager.UpdateAsync(employee);

            await _emailService.SendEmailAsync(
                employee.Email,
                "Onboarding Complete - Welcome to ALBA!",
                $"<p>Hi {employee.FullName},</p>" +
                $"<p>Congratulations! Your onboarding is now <b>complete</b>. Welcome to ALBA!</p>");

            TempData["Success"] = $"{employee.FullName}'s onboarding marked as complete.";
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Checklists()
        {
            var items = await _db.ChecklistItems
                .OrderBy(i => i.DisplayOrder).ToListAsync();
            ViewBag.ChecklistItems = items;
            return View();
        }

        public IActionResult Employees()
        {
            return RedirectToAction("Dashboard");
        }

        private async Task CheckAndUpdateOnboardingStatus(string employeeId)
        {
            var mandatoryItems = await _db.ChecklistItems
                .Where(i => i.IsMandatory).ToListAsync();
            var approvedIds = await _db.ChecklistSubmissions
                .Where(s => s.EmployeeId == employeeId
                    && s.Status == SubmissionStatus.Approved)
                .Select(s => s.ChecklistItemId).ToListAsync();

            var allApproved = mandatoryItems.All(i => approvedIds.Contains(i.Id));
            var employee = await _userManager.FindByIdAsync(employeeId);

            employee.OnboardingStatus = allApproved
                ? OnboardingStatus.Completed
                : OnboardingStatus.InProgress;

            await _userManager.UpdateAsync(employee);
        }
        public async Task<IActionResult> ViewDocument(int submissionId)
        {
            var currentHR = await _userManager.GetUserAsync(User);
            var submission = await _db.ChecklistSubmissions
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return NotFound();

            // Only allow HR assigned to this employee
            if (submission.Employee.AssignedHRId != currentHR.Id)
                return Forbid();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(),
                "SecureUploads", submission.FilePath);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var ext = Path.GetExtension(submission.FileName).ToLower();
            var contentType = ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, submission.FileName);
        }
    }
}
