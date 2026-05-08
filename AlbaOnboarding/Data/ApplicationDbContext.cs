using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AlbaOnboarding.Models;

namespace AlbaOnboarding.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InvitationToken> InvitationTokens { get; set; }
        public DbSet<ChecklistItem> ChecklistItems { get; set; }
        public DbSet<ChecklistSubmission> ChecklistSubmissions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // This loop finds every string property in your Identity tables 
            // and ensures they use MySQL-compatible lengths instead of "max"
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string))
                    {
                        // If it's a key (like Id), set it to 255 (MySQL index limit)
                        if (property.IsKey() || property.IsForeignKey())
                        {
                            property.SetMaxLength(255);
                        }
                        // If it's a normal string that was "max", make it a longtext
                        else if (property.GetMaxLength() == null)
                        {
                            property.SetColumnType("longtext");
                        }
                    }
                }
            }
        }
    }
}
                
            
        
    
