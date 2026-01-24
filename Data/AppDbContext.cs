using khidma_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ================== DbSets (Tables) ==================
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<Bid> Bids { get; set; } = null!;
    public DbSet<Contract> Contracts { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<ChatbotLog> ChatbotLogs { get; set; } = null!;
    public DbSet<Skill> Skills { get; set; } = null!;
    public DbSet<UserSkill> UserSkills { get; set; } = null!;
    public DbSet<OtpCode> OtpCodes { get; set; } = null!;
    public DbSet<PhoneVerification> PhoneVerifications { get; set; } = null!;
    public DbSet<EmailVerification> EmailVerifications { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<JobComment> JobComments { get; set; } = null!;
    public DbSet<UserConnection> UserConnections { get; set; } = null!;
    public DbSet<UserAiProfile> UserAiProfiles { get; set; } = null!;
    public DbSet<SocialPost> SocialPosts { get; set; } = null!;
    public DbSet<PostLike> PostLikes { get; set; } = null!;
    public DbSet<PostComment> PostComments { get; set; } = null!;
    public DbSet<JobUpdate> JobUpdates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ========== User Configuration ==========
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.UserType).HasConversion<int>();
            entity.Property(e => e.Balance).HasColumnType("decimal(18,2)");
        });

        // ========== Password Reset Token ==========
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
        });

        // User Relationships
        modelBuilder.Entity<Job>().HasOne(j => j.Client).WithMany(u => u.Jobs).HasForeignKey(j => j.ClientId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Bid>().HasOne(b => b.Freelancer).WithMany(u => u.Bids).HasForeignKey(b => b.FreelancerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>().HasOne(r => r.Reviewer).WithMany(u => u.ReviewsWritten).HasForeignKey(r => r.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Review>().HasOne(r => r.Reviewee).WithMany(u => u.ReviewsReceived).HasForeignKey(r => r.RevieweeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChatbotLog>().HasOne(c => c.User).WithMany(u => u.ChatbotLogs).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Contract>().HasOne(c => c.Freelancer).WithMany(u => u.ContractsAsFreelancer).HasForeignKey(c => c.FreelancerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Contract>().HasOne(c => c.Client).WithMany(u => u.ContractsAsClient).HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.Restrict);

        // ========== Conversation / Message Configuration ==========
        modelBuilder.Entity<Conversation>().HasOne(c => c.User1).WithMany().HasForeignKey(c => c.User1Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Conversation>().HasOne(c => c.User2).WithMany().HasForeignKey(c => c.User2Id).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Message>(entity => { entity.HasKey(e => e.MessageId); });
        modelBuilder.Entity<Message>().HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Message>().HasOne(m => m.Sender).WithMany(u => u.SentMessages).HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);

        // ========== Job / Bid / Contract / Payment (Enums & Decimals) ==========
        modelBuilder.Entity<Job>(entity => {
            entity.HasKey(e => e.JobId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Budget).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Bid>(entity => {
            entity.HasKey(e => e.BidId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.BidAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Contract>(entity => {
            entity.HasKey(e => e.ContractId);
            entity.Property(e => e.StartDate).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.EscrowAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Payment>(entity => {
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.PaymentMethod).HasConversion<int>();
            entity.Property(e => e.PaymentStatus).HasConversion<int>();
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });

        // ========== ✅ User Connections (Critical Fix) ==========
        modelBuilder.Entity<UserConnection>(entity => {
            entity.HasKey(e => e.ConnectionId);
            entity.Property(e => e.Status).HasConversion<int>(); // Maps enum to int for SQL queries
            entity.HasOne(c => c.Requester).WithMany().HasForeignKey(c => c.RequesterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.Receiver).WithMany().HasForeignKey(c => c.ReceiverId).OnDelete(DeleteBehavior.Restrict);
        });

        // ========== ✅ Social Feed Configuration (Critical Fix) ==========
        modelBuilder.Entity<SocialPost>(entity => {
            entity.HasKey(e => e.PostId);
            entity.Property(e => e.Type).HasConversion<int>(); // Maps enum to int for SQL queries
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostLike>(entity => {
            entity.HasKey(e => e.LikeId);
            entity.HasOne(pl => pl.Post).WithMany(p => p.Likes).HasForeignKey(pl => pl.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pl => pl.User).WithMany().HasForeignKey(pl => pl.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostComment>(entity => {
            entity.HasKey(e => e.CommentId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.HasOne(pc => pc.Post).WithMany(p => p.Comments).HasForeignKey(pc => pc.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pc => pc.User).WithMany().HasForeignKey(pc => pc.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ========== ✅ Job Updates (Workspace) Configuration ==========
        modelBuilder.Entity<JobUpdate>(entity => {
            entity.HasKey(e => e.UpdateId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.HasOne(ju => ju.Job).WithMany().HasForeignKey(ju => ju.JobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ju => ju.Freelancer).WithMany().HasForeignKey(ju => ju.FreelancerId).OnDelete(DeleteBehavior.Restrict);
        });

        // Remaining Tables
        modelBuilder.Entity<Review>(entity => { entity.HasKey(e => e.ReviewId); entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)"); });
        modelBuilder.Entity<ChatbotLog>(entity => { entity.HasKey(e => e.ChatId); entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP(6)"); });
        modelBuilder.Entity<Skill>(entity => { entity.HasKey(e => e.SkillId); entity.HasIndex(e => e.SkillName).IsUnique(); });
        modelBuilder.Entity<UserSkill>(entity => { 
            entity.HasKey(e => new { e.UserId, e.SkillId });
            entity.HasOne(us => us.User).WithMany(u => u.UserSkills).HasForeignKey(us => us.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(us => us.Skill).WithMany(s => s.UserSkills).HasForeignKey(us => us.SkillId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Notification>(entity => { 
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}