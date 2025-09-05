using Microsoft.EntityFrameworkCore;
using AiGrader.Models;

namespace AiGrader.Data;

public class AiGraderDbContext : DbContext
{
    public AiGraderDbContext(DbContextOptions<AiGraderDbContext> options) : base(options)
    {
    }
    
    public DbSet<DbCourse> Courses { get; set; }
    public DbSet<DbAssignment> Assignments { get; set; }
    public DbSet<DbSubmission> Submissions { get; set; }
    public DbSet<DbAttachment> Attachments { get; set; }
    public DbSet<DbAnalysisResult> AnalysisResults { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<DbCourse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Assignments)
                  .WithOne(e => e.Course)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<DbAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Submissions)
                  .WithOne(e => e.Assignment)
                  .HasForeignKey(e => e.AssignmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<DbSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Attachments)
                  .WithOne(e => e.Submission)
                  .HasForeignKey(e => e.SubmissionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        modelBuilder.Entity<DbAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<DbAnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Assignment)
                  .WithMany()
                  .HasForeignKey(e => e.AssignmentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}