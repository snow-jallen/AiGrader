using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AiGrader.Models;

public class DbCourse
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? CustomName { get; set; }
    
    [MaxLength(100)]
    public string CourseCode { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string WorkflowState { get; set; } = string.Empty;
    
    public DateTime LastSynced { get; set; } = DateTime.UtcNow;
    
    public bool IsHidden { get; set; } = false;
    
    public List<DbAssignment> Assignments { get; set; } = new();
    
    [NotMapped]
    public string DisplayName => !string.IsNullOrWhiteSpace(CustomName) ? CustomName : Name;
    
    public static DbCourse FromCanvas(Course canvasCourse)
    {
        return new DbCourse
        {
            Id = canvasCourse.Id,
            Name = canvasCourse.Name,
            CourseCode = canvasCourse.CourseCode,
            WorkflowState = canvasCourse.WorkflowState
        };
    }
    
    public Course ToCanvas()
    {
        return new Course
        {
            Id = Id,
            Name = Name,
            CourseCode = CourseCode,
            WorkflowState = WorkflowState
        };
    }
}

public class DbAssignment
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    public long CourseId { get; set; }
    
    public DateTime? DueAt { get; set; }
    
    public bool Published { get; set; } = false;
    
    public double? PointsPossible { get; set; }
    
    public int UngradedCount { get; set; } = 0;
    
    public int TotalSubmissions { get; set; } = 0;
    
    public DateTime LastSynced { get; set; } = DateTime.UtcNow;
    
    public bool HasDownloadedSubmissions { get; set; } = false;
    
    public string? LocalSubmissionsPath { get; set; }
    
    [ForeignKey(nameof(CourseId))]
    public DbCourse Course { get; set; } = null!;
    
    public List<DbSubmission> Submissions { get; set; } = new();
    
    [NotMapped]
    public bool IsOverdue => DueAt.HasValue && DueAt.Value < DateTime.UtcNow && Published;
    
    [NotMapped]
    public bool HasUngraded => UngradedCount > 0;
    
    public static DbAssignment FromCanvas(Assignment canvasAssignment)
    {
        return new DbAssignment
        {
            Id = canvasAssignment.Id,
            Name = canvasAssignment.Name,
            CourseId = canvasAssignment.CourseId,
            DueAt = canvasAssignment.DueAt,
            Published = canvasAssignment.Published,
            PointsPossible = canvasAssignment.PointsPossible
        };
    }
    
    public Assignment ToCanvas()
    {
        return new Assignment
        {
            Id = Id,
            Name = Name,
            CourseId = CourseId,
            DueAt = DueAt,
            Published = Published,
            PointsPossible = PointsPossible
        };
    }
}

public class DbSubmission
{
    [Key]
    public long Id { get; set; }
    
    public long UserId { get; set; }
    
    public long AssignmentId { get; set; }
    
    public string? Body { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    
    [MaxLength(50)]
    public string WorkflowState { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string StudentName { get; set; } = string.Empty;
    
    public string? LocalFilePath { get; set; }
    
    public DateTime LastSynced { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(AssignmentId))]
    public DbAssignment Assignment { get; set; } = null!;
    
    public List<DbAttachment> Attachments { get; set; } = new();
    
    public static DbSubmission FromCanvas(Submission canvasSubmission, string studentName)
    {
        return new DbSubmission
        {
            Id = canvasSubmission.Id,
            UserId = canvasSubmission.UserId,
            AssignmentId = canvasSubmission.AssignmentId,
            Body = canvasSubmission.Body,
            SubmittedAt = canvasSubmission.SubmittedAt,
            WorkflowState = canvasSubmission.WorkflowState,
            StudentName = studentName
        };
    }
    
    public Submission ToCanvas()
    {
        return new Submission
        {
            Id = Id,
            UserId = UserId,
            AssignmentId = AssignmentId,
            Body = Body,
            SubmittedAt = SubmittedAt,
            WorkflowState = WorkflowState,
            Attachments = Attachments.Select(a => a.ToCanvas()).ToList()
        };
    }
}

public class DbAttachment
{
    [Key]
    public long Id { get; set; }
    
    public long SubmissionId { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Filename { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Url { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    public string? LocalFilePath { get; set; }
    
    public bool IsDownloaded { get; set; } = false;
    
    [ForeignKey(nameof(SubmissionId))]
    public DbSubmission Submission { get; set; } = null!;
    
    public static DbAttachment FromCanvas(Attachment canvasAttachment, long submissionId)
    {
        return new DbAttachment
        {
            Id = canvasAttachment.Id,
            SubmissionId = submissionId,
            Filename = canvasAttachment.Filename,
            Url = canvasAttachment.Url,
            ContentType = canvasAttachment.ContentType
        };
    }
    
    public Attachment ToCanvas()
    {
        return new Attachment
        {
            Id = Id,
            Filename = Filename,
            Url = Url,
            ContentType = ContentType
        };
    }
}

public class DbAnalysisResult
{
    [Key]
    public int Id { get; set; }
    
    public long AssignmentId { get; set; }
    
    public string AnalysisJson { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(AssignmentId))]
    public DbAssignment Assignment { get; set; } = null!;
}