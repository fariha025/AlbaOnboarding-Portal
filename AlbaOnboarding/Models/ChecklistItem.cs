namespace AlbaOnboarding.Models
{
    public class ChecklistItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsMandatory { get; set; } = true;
        public string Department { get; set; } = "All";
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}