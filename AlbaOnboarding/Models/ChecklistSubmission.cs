namespace AlbaOnboarding.Models
{
    public class ChecklistSubmission
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = "";
        public ApplicationUser? Employee { get; set; }
        public int ChecklistItemId { get; set; }
        public ChecklistItem? ChecklistItem { get; set; }
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public string? ReviewComment { get; set; }
    }

    public enum SubmissionStatus
    {
        Pending,
        Uploaded,
        Approved,
        Rejected
    }
}
