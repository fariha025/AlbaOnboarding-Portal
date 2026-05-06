using AlbaOnboarding.Data;
using AlbaOnboarding.Models;
using AlbaOnboarding.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlbaOnboarding.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly EmailService _emailService;

        public EmployeeController(UserManager<ApplicationUser> userManager,
            ApplicationDbContext db, EmailService emailService)
        {
            _userManager = userManager;
            _db = db;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var allItems = await _db.ChecklistItems.OrderBy(c => c.DisplayOrder).ToListAsync();
            var submissions = await _db.ChecklistSubmissions
                .Where(s => s.EmployeeId == user.Id).ToListAsync();

            if (!string.IsNullOrEmpty(user.AssignedHRId))
            {
                var hr = await _userManager.FindByIdAsync(user.AssignedHRId);
                ViewBag.AssignedHR = hr?.FullName ?? "Not assigned";
            }
            else
            {
                ViewBag.AssignedHR = "Not assigned yet";
            }
            ViewBag.User = user;
            ViewBag.ChecklistItems = allItems;
            ViewBag.Submissions = submissions;
            ViewBag.TotalItems = allItems.Count;
            ViewBag.ApprovedCount = submissions.Count(s => s.Status == SubmissionStatus.Approved);
            ViewBag.PendingCount = allItems.Count - submissions.Count(s => s.Status == SubmissionStatus.Approved);

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
     string fullName, string department, string jobTitle,
     string phoneNumber, string cprNumber, string badgeNumber,
     string prfNumber, string grade, string referenceNumber,
     string source, string preparedBy, string reviewedBy,
     string startDate)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                TempData["Error"] = "Full name is required.";
                var u = await _userManager.GetUserAsync(User);
                return View(u);
            }

            var user = await _userManager.GetUserAsync(User);
            user.FullName = fullName;
            user.Department = department ?? "";
            user.JobTitle = jobTitle ?? "";
            user.PhoneNumber = phoneNumber ?? "";
            user.CprNumber = cprNumber ?? "";
            user.BadgeNumber = badgeNumber ?? "";
            user.PrfNumber = prfNumber ?? "";
            user.Grade = grade ?? "";
            user.ReferenceNumber = referenceNumber ?? "";
            user.Source = source ?? "";
            user.PreparedBy = preparedBy ?? "";
            user.ReviewedByName = reviewedBy ?? "";

            if (!string.IsNullOrEmpty(startDate) &&
                DateTime.TryParse(startDate, out DateTime parsedDate))
                user.StartDate = parsedDate;

            // Calculate profile completion
            int filled = 0;
            int total = 8;
            if (!string.IsNullOrEmpty(user.FullName)) filled++;
            if (!string.IsNullOrEmpty(user.Department)) filled++;
            if (!string.IsNullOrEmpty(user.JobTitle)) filled++;
            if (!string.IsNullOrEmpty(user.PhoneNumber)) filled++;
            if (!string.IsNullOrEmpty(user.CprNumber)) filled++;
            if (!string.IsNullOrEmpty(user.BadgeNumber)) filled++;
            if (!string.IsNullOrEmpty(user.Grade)) filled++;
            if (user.StartDate != default) filled++;
            user.ProfileCompletionPercent = (filled * 100) / total;

            if (user.OnboardingStatus == OnboardingStatus.NotStarted)
                user.OnboardingStatus = OnboardingStatus.InProgress;

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int checklistItemId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction("Dashboard");
            }

            // Validate file type
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Only PDF, JPG, and PNG files are allowed.";
                return RedirectToAction("Dashboard");
            }

            // Validate file size (5MB max)
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size must be under 5MB.";
                return RedirectToAction("Dashboard");
            }

            var user = await _userManager.GetUserAsync(User);

            // Save file to SecureUploads/{userId}/ outside wwwroot
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),
                "SecureUploads", user.Id);
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{checklistItemId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // Store relative path for retrieval
            var relativePath = Path.Combine(user.Id, fileName);

            // Save or update submission
            var existing = await _db.ChecklistSubmissions
                .FirstOrDefaultAsync(s => s.EmployeeId == user.Id
                    && s.ChecklistItemId == checklistItemId);

            if (existing != null)
            {
                existing.FileName = file.FileName;
                existing.FilePath = relativePath;
                existing.Status = SubmissionStatus.Uploaded;
                existing.SubmittedAt = DateTime.Now;
            }
            else
            {
                _db.ChecklistSubmissions.Add(new ChecklistSubmission
                {
                    EmployeeId = user.Id,
                    ChecklistItemId = checklistItemId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    Status = SubmissionStatus.Uploaded,
                    SubmittedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction("Dashboard");
        }

        public IActionResult Documents()
        {
            return RedirectToAction("Dashboard");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Please select a photo.";
                return RedirectToAction("Profile");
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(photo.FileName).ToLower();
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Only JPG and PNG photos are allowed.";
                return RedirectToAction("Profile");
            }

            if (photo.Length > 2 * 1024 * 1024)
            {
                TempData["Error"] = "Photo must be under 2MB.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);
            var folder = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot", "photos");
            Directory.CreateDirectory(folder);

            var fileName = $"{user.Id}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            user.PhotoPath = $"/photos/{fileName}";
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Photo uploaded successfully.";
            return RedirectToAction("Profile");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto()
        {
            var user = await _userManager.GetUserAsync(User);

            if (!string.IsNullOrEmpty(user.PhotoPath))
            {
                // Delete the physical file
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(),
                    "wwwroot", user.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);

                // Clear the path in database
                user.PhotoPath = null;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = "Profile photo removed.";
            return RedirectToAction("Profile");
        }
    }
}