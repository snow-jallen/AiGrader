using Microsoft.EntityFrameworkCore;
using AiGrader.Data;
using AiGrader.Models;

namespace AiGrader.Services;

public class DatabaseService : IDatabaseService
{
    private readonly AiGraderDbContext _context;
    
    public DatabaseService(AiGraderDbContext context)
    {
        _context = context;
    }
    
    public async Task InitializeDatabaseAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }
    
    public async Task<List<DbCourse>> GetCoursesAsync(bool includeHidden = false)
    {
        var query = _context.Courses
            .AsNoTracking() // Always get fresh data from database
            .Include(c => c.Assignments)
            .AsQueryable();
        
        if (!includeHidden)
        {
            query = query.Where(c => !c.IsHidden);
        }
        
        var courses = await query
            .OrderBy(c => c.Name)
            .ToListAsync();
            
        // Debug logging
        foreach (var course in courses)
        {
            Console.WriteLine($"Retrieved course {course.Id}: Name='{course.Name}', CustomName='{course.CustomName}', DisplayName='{course.DisplayName}'");
        }
        
        return courses;
    }
    
    public async Task<List<DbAssignment>> GetAssignmentsAsync(long? courseId = null)
    {
        var query = _context.Assignments.AsQueryable();
        
        if (courseId.HasValue)
        {
            query = query.Where(a => a.CourseId == courseId.Value);
        }
        
        return await query
            .Include(a => a.Course)
            .OrderBy(a => a.Course.Name)
            .ThenBy(a => a.Name)
            .ToListAsync();
    }
    
    public async Task<DbAssignment?> GetAssignmentAsync(long assignmentId)
    {
        return await _context.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .ThenInclude(s => s.Attachments)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
    }
    
    public async Task<List<DbSubmission>> GetSubmissionsAsync(long assignmentId)
    {
        return await _context.Submissions
            .Include(s => s.Attachments)
            .Where(s => s.AssignmentId == assignmentId)
            .OrderBy(s => s.StudentName)
            .ToListAsync();
    }
    
    public async Task SyncCoursesAsync(List<Course> canvasCourses)
    {
        foreach (var canvasCourse in canvasCourses)
        {
            var existingCourse = await _context.Courses.FindAsync(canvasCourse.Id);
            
            if (existingCourse == null)
            {
                var dbCourse = DbCourse.FromCanvas(canvasCourse);
                _context.Courses.Add(dbCourse);
            }
            else
            {
                existingCourse.Name = canvasCourse.Name;
                existingCourse.CourseCode = canvasCourse.CourseCode;
                existingCourse.WorkflowState = canvasCourse.WorkflowState;
                existingCourse.LastSynced = DateTime.UtcNow;
                _context.Courses.Update(existingCourse);
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task SyncAssignmentsAsync(long courseId, List<Assignment> canvasAssignments)
    {
        var existingAssignments = await _context.Assignments
            .Where(a => a.CourseId == courseId)
            .ToListAsync();
        
        var existingIds = existingAssignments.Select(a => a.Id).ToHashSet();
        var canvasIds = canvasAssignments.Select(a => a.Id).ToHashSet();
        
        var toRemove = existingAssignments.Where(a => !canvasIds.Contains(a.Id)).ToList();
        _context.Assignments.RemoveRange(toRemove);
        
        foreach (var canvasAssignment in canvasAssignments)
        {
            var existingAssignment = existingAssignments.FirstOrDefault(a => a.Id == canvasAssignment.Id);
            
            if (existingAssignment == null)
            {
                var dbAssignment = DbAssignment.FromCanvas(canvasAssignment);
                _context.Assignments.Add(dbAssignment);
            }
            else
            {
                existingAssignment.Name = canvasAssignment.Name;
                existingAssignment.DueAt = canvasAssignment.DueAt;
                existingAssignment.Published = canvasAssignment.Published;
                existingAssignment.PointsPossible = canvasAssignment.PointsPossible;
                existingAssignment.LastSynced = DateTime.UtcNow;
                _context.Assignments.Update(existingAssignment);
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task SyncSubmissionsAsync(long assignmentId, List<Submission> canvasSubmissions, Dictionary<long, User> users)
    {
        var existingSubmissions = await _context.Submissions
            .Include(s => s.Attachments)
            .Where(s => s.AssignmentId == assignmentId)
            .ToListAsync();
        
        var existingIds = existingSubmissions.Select(s => s.Id).ToHashSet();
        var canvasIds = canvasSubmissions.Select(s => s.Id).ToHashSet();
        
        var toRemove = existingSubmissions.Where(s => !canvasIds.Contains(s.Id)).ToList();
        _context.Submissions.RemoveRange(toRemove);
        
        foreach (var canvasSubmission in canvasSubmissions)
        {
            var studentName = users.TryGetValue(canvasSubmission.UserId, out var user) ? user.Name : $"User {canvasSubmission.UserId}";
            var existingSubmission = existingSubmissions.FirstOrDefault(s => s.Id == canvasSubmission.Id);
            
            if (existingSubmission == null)
            {
                var dbSubmission = DbSubmission.FromCanvas(canvasSubmission, studentName);
                _context.Submissions.Add(dbSubmission);
                
                if (canvasSubmission.Attachments?.Any() == true)
                {
                    foreach (var attachment in canvasSubmission.Attachments)
                    {
                        var dbAttachment = DbAttachment.FromCanvas(attachment, canvasSubmission.Id);
                        _context.Attachments.Add(dbAttachment);
                    }
                }
            }
            else
            {
                existingSubmission.Body = canvasSubmission.Body;
                existingSubmission.SubmittedAt = canvasSubmission.SubmittedAt;
                existingSubmission.WorkflowState = canvasSubmission.WorkflowState;
                existingSubmission.StudentName = studentName;
                existingSubmission.LastSynced = DateTime.UtcNow;
                _context.Submissions.Update(existingSubmission);
                
                var existingAttachmentIds = existingSubmission.Attachments.Select(a => a.Id).ToHashSet();
                var canvasAttachmentIds = canvasSubmission.Attachments?.Select(a => a.Id).ToHashSet() ?? new HashSet<long>();
                
                var attachmentsToRemove = existingSubmission.Attachments.Where(a => !canvasAttachmentIds.Contains(a.Id)).ToList();
                _context.Attachments.RemoveRange(attachmentsToRemove);
                
                if (canvasSubmission.Attachments?.Any() == true)
                {
                    foreach (var attachment in canvasSubmission.Attachments)
                    {
                        var existingAttachment = existingSubmission.Attachments.FirstOrDefault(a => a.Id == attachment.Id);
                        if (existingAttachment == null)
                        {
                            var dbAttachment = DbAttachment.FromCanvas(attachment, canvasSubmission.Id);
                            _context.Attachments.Add(dbAttachment);
                        }
                    }
                }
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> NeedsInitialSync()
    {
        return !await _context.Courses.AnyAsync();
    }
    
    public async Task<DateTime?> GetLastSyncTimeAsync(long? courseId = null)
    {
        if (courseId.HasValue)
        {
            var course = await _context.Courses.FindAsync(courseId.Value);
            return course?.LastSynced;
        }
        
        var lastSync = await _context.Courses
            .OrderByDescending(c => c.LastSynced)
            .Select(c => c.LastSynced)
            .FirstOrDefaultAsync();
        
        return lastSync == default ? null : lastSync;
    }
    
    public async Task SaveAnalysisResultAsync(long assignmentId, string analysisJson)
    {
        var existingResult = await _context.AnalysisResults
            .Where(r => r.AssignmentId == assignmentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
        
        var newResult = new DbAnalysisResult
        {
            AssignmentId = assignmentId,
            AnalysisJson = analysisJson,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.AnalysisResults.Add(newResult);
        await _context.SaveChangesAsync();
    }
    
    public async Task<string?> GetLatestAnalysisResultAsync(long assignmentId)
    {
        var result = await _context.AnalysisResults
            .Where(r => r.AssignmentId == assignmentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
        
        return result?.AnalysisJson;
    }
    
    public async Task UpdateSubmissionLocalPathAsync(long submissionId, string localPath)
    {
        var submission = await _context.Submissions.FindAsync(submissionId);
        if (submission != null)
        {
            submission.LocalFilePath = localPath;
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UpdateAttachmentDownloadStatusAsync(long attachmentId, string localPath)
    {
        var attachment = await _context.Attachments.FindAsync(attachmentId);
        if (attachment != null)
        {
            attachment.LocalFilePath = localPath;
            attachment.IsDownloaded = true;
            _context.Attachments.Update(attachment);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UpdateAssignmentSubmissionsDownloadedAsync(long assignmentId, string localPath)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            assignment.HasDownloadedSubmissions = true;
            assignment.LocalSubmissionsPath = localPath;
            _context.Assignments.Update(assignment);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task SetCourseHiddenStatusAsync(long courseId, bool isHidden)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course != null)
        {
            course.IsHidden = isHidden;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task SetCourseCustomNameAsync(long courseId, string? customName)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course != null)
        {
            var trimmedName = string.IsNullOrWhiteSpace(customName) ? null : customName.Trim();
            course.CustomName = trimmedName;
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
            
            // Debug logging
            Console.WriteLine($"Updated course {courseId}: Name='{course.Name}', CustomName='{course.CustomName}', DisplayName='{course.DisplayName}'");
            
            // Clear the entire change tracker to prevent caching issues
            _context.ChangeTracker.Clear();
        }
    }
    
    public async Task UpdateAssignmentStatsAsync(long assignmentId, int totalSubmissions, int ungradedCount)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            assignment.TotalSubmissions = totalSubmissions;
            assignment.UngradedCount = ungradedCount;
            _context.Assignments.Update(assignment);
            await _context.SaveChangesAsync();
        }
    }
}