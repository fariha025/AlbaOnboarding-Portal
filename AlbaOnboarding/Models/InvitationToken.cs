namespace AlbaOnboarding.Models
{
    public class InvitationToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(7);
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
