using AiGrader.Models;

namespace AiGrader.Services;

public interface IDatabaseService
{
    Task InitializeDatabaseAsync();
    
    Task<List<DbCourse>> GetCoursesAsync(bool includeHidden = false);
    Task<List<DbAssignment>> GetAssignmentsAsync(long? courseId = null);
    Task<DbAssignment?> GetAssignmentAsync(long assignmentId);
    Task<List<DbSubmission>> GetSubmissionsAsync(long assignmentId);
    
    Task SyncCoursesAsync(List<Course> canvasCourses);
    Task SyncAssignmentsAsync(long courseId, List<Assignment> canvasAssignments);
    Task SyncSubmissionsAsync(long assignmentId, List<Submission> canvasSubmissions, Dictionary<long, User> users);
    
    Task<bool> NeedsInitialSync();
    Task<DateTime?> GetLastSyncTimeAsync(long? courseId = null);
    
    Task SaveAnalysisResultAsync(long assignmentId, string analysisJson);
    Task<string?> GetLatestAnalysisResultAsync(long assignmentId);
    
    Task UpdateSubmissionLocalPathAsync(long submissionId, string localPath);
    Task UpdateAttachmentDownloadStatusAsync(long attachmentId, string localPath);
    Task UpdateAssignmentSubmissionsDownloadedAsync(long assignmentId, string localPath);
    
    Task SetCourseHiddenStatusAsync(long courseId, bool isHidden);
    Task SetCourseCustomNameAsync(long courseId, string? customName);
    Task UpdateAssignmentStatsAsync(long assignmentId, int totalSubmissions, int ungradedCount);
}