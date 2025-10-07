using khidma_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace khidma_backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<Bid> Bids { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Defaults and constraints
        modelBuilder.Entity<User>()
            .Property(u => u.DateCreated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        modelBuilder.Entity<Job>()
            .Property(j => j.PostedDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        modelBuilder.Entity<Bid>()
            .Property(b => b.BidDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.PaymentDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        modelBuilder.Entity<Review>()
            .Property(r => r.ReviewDate)
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

        // User (Client) -> Jobs (one-to-many)
        modelBuilder.Entity<Job>()
            .HasOne(j => j.Client)
            .WithMany(u => u.JobsPosted!)
            .HasForeignKey(j => j.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Job -> Bids (one-to-many)
        modelBuilder.Entity<Bid>()
            .HasOne(b => b.Job)
            .WithMany(j => j.Bids!)
            .HasForeignKey(b => b.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // User (Freelancer) -> Bids (one-to-many)
        modelBuilder.Entity<Bid>()
            .HasOne(b => b.Freelancer)
            .WithMany(u => u.BidsPlaced!)
            .HasForeignKey(b => b.FreelancerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Job -> Payment (one-to-one)
        modelBuilder.Entity<Job>()
            .HasOne(j => j.Payment)
            .WithOne(p => p.Job!)
            .HasForeignKey<Payment>(p => p.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Reviews: User -> ReviewsGiven (one-to-many via Reviewer)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany(u => u.ReviewsGiven!)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reviews: User -> ReviewsReceived (one-to-many via ReviewedUser)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.ReviewedUser)
            .WithMany(u => u.ReviewsReceived!)
            .HasForeignKey(r => r.ReviewedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review -> Job (many-to-one)
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Job)
            .WithMany() // no navigation on Job for reviews
            .HasForeignKey(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


