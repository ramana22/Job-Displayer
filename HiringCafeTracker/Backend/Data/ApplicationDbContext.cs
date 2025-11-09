using HiringCafeTracker.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace HiringCafeTracker.Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Resume> Resumes => Set<Resume>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Job>()
            .HasIndex(j => j.JobId)
            .IsUnique();

        modelBuilder.Entity<Job>()
            .Property(j => j.MatchingScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Job>()
            .Property(j => j.Status)
            .HasDefaultValue("Not Applied");

        modelBuilder.Entity<Job>()
            .Property(j => j.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<Resume>()
            .Property(r => r.UploadedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
