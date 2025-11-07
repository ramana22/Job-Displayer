using JobDisplayer.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JobDisplayer.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<ResumeFile> Resumes => Set<ResumeFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobPosting>()
            .Property(j => j.JobTitle)
            .HasMaxLength(256);

        modelBuilder.Entity<JobPosting>()
            .Property(j => j.Company)
            .HasMaxLength(256);

        modelBuilder.Entity<JobPosting>()
            .Property(j => j.Location)
            .HasMaxLength(256);

        modelBuilder.Entity<JobPosting>()
            .Property(j => j.SearchKey)
            .HasMaxLength(256);

        modelBuilder.Entity<JobPosting>()
            .Property(j => j.ApplyLink)
            .HasMaxLength(512);

        modelBuilder.Entity<ResumeFile>()
            .Property(r => r.FileName)
            .HasMaxLength(256);

        modelBuilder.Entity<ResumeFile>()
            .Property(r => r.ContentType)
            .HasMaxLength(128);
    }
}
