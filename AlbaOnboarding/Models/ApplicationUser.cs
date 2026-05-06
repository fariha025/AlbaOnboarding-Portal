using Microsoft.AspNetCore.Identity;

namespace AlbaOnboarding.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = "";
        public string Department { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public DateTime StartDate { get; set; }
        public int ProfileCompletionPercent { get; set; } = 0;
        public OnboardingStatus OnboardingStatus { get; set; } = OnboardingStatus.NotStarted;
        public string? AssignedHRId { get; set; }
        public string? CprNumber { get; set; }
        public string? BadgeNumber { get; set; }
        public string? PrfNumber { get; set; }
        public string? Grade { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Source { get; set; }
        public string? PreparedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public string? PhotoPath { get; set; }
    }

    public enum OnboardingStatus
    {
        NotStarted,
        InProgress,
        Completed
    }
}