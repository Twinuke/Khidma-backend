using khidma_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
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
        });

        // User -> Jobs (one-to-many)
        modelBuilder.Entity<Job>()
            .HasOne(j => j.Client)
            .WithMany(u => u.Jobs)
            .HasForeignKey(j => j.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Bids (one-to-many)
        modelBuilder.Entity<Bid>()
            .HasOne(b => b.Freelancer)
            .WithMany(u => u.Bids)
            .HasForeignKey(b => b.FreelancerId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> ReviewsWritten (one-to-many)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany(u => u.ReviewsWritten)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> ReviewsReceived (one-to-many)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewee)
            .WithMany(u => u.ReviewsReceived)
            .HasForeignKey(r => r.RevieweeId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> SentMessages (one-to-many)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> ReceivedMessages (one-to-many)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> ChatbotLogs (one-to-many)
        modelBuilder.Entity<ChatbotLog>()
            .HasOne(c => c.User)
            .WithMany(u => u.ChatbotLogs)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> ContractsAsFreelancer (one-to-many)
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Freelancer)
            .WithMany(u => u.ContractsAsFreelancer)
            .HasForeignKey(c => c.FreelancerId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> ContractsAsClient (one-to-many)
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Client)
            .WithMany(u => u.ContractsAsClient)
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // ========== Job Configuration ==========
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.JobId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Budget).HasPrecision(18, 2);
        });

        // Job -> Bids (one-to-many)
        modelBuilder.Entity<Bid>()
            .HasOne(b => b.Job)
            .WithMany(j => j.Bids)
            .HasForeignKey(b => b.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job -> Contracts (one-to-many)
        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Job)
            .WithMany(j => j.Contracts)
            .HasForeignKey(c => c.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job -> Messages (one-to-many, optional)
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Job)
            .WithMany(j => j.Messages)
            .HasForeignKey(m => m.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        // ========== Bid Configuration ==========
        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.BidId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.BidAmount).HasPrecision(18, 2);
        });

        // ========== Contract Configuration ==========
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId);
            entity.Property(e => e.StartDate).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.EscrowAmount).HasPrecision(18, 2);
        });

        // Contract -> Payments (one-to-many)
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Contract)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        // Contract -> Reviews (one-to-many)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Contract)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        // ========== Payment Configuration ==========
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            entity.Property(e => e.PaymentMethod).HasConversion<int>();
            entity.Property(e => e.PaymentStatus).HasConversion<int>();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        // ========== Review Configuration ==========
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        // ========== Message Configuration ==========
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        // ========== ChatbotLog Configuration ==========
        modelBuilder.Entity<ChatbotLog>(entity =>
        {
            entity.HasKey(e => e.ChatId);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        // ========== Skill Configuration ==========
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId);
            entity.HasIndex(e => e.SkillName).IsUnique();
        });

        // ========== UserSkill Configuration (Many-to-Many) ==========
        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.SkillId });

            entity.HasOne(us => us.User)
                .WithMany(u => u.UserSkills)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(us => us.Skill)
                .WithMany(s => s.UserSkills)
                .HasForeignKey(us => us.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
