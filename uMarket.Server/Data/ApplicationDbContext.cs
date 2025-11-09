using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using uMarket.Server.Models;
using uMarket.Server.Models.Chat;

namespace uMarket.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ChatRequest> ChatRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Ensure InstitutionalId is unique if set
            builder.Entity<ApplicationUser>(b =>
            {
                b.HasIndex(u => u.InstitutionalId).IsUnique();
                b.HasIndex(u => u.IsAvailable); // Optional: Index for IsAvailable
            });

            // ConversationParticipant composite key
            builder.Entity<ConversationParticipant>(b =>
            {
                b.HasKey(cp => new { cp.ConversationId, cp.UserId });
                b.HasOne(cp => cp.Conversation).WithMany(c => c.Participants).HasForeignKey(cp => cp.ConversationId);
                // No direct FK to ApplicationUser here due to Identity schema string key, but can add if desired
            });

            // Message relationships
            builder.Entity<Message>(b =>
            {
                b.HasKey(m => m.Id);
                b.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(m => new { m.ConversationId, m.SentAt });
            });

            // Conversation
            builder.Entity<Conversation>(b =>
            {
                b.HasKey(c => c.Id);
                b.HasMany(c => c.Participants).WithOne(p => p.Conversation).HasForeignKey(p => p.ConversationId);
            });

            // ChatRequest
            builder.Entity<ChatRequest>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasIndex(r => new { r.ToUserId, r.Status });
            });
        }
    }
}
