using AiGrader.Models;

namespace AiGrader.Services;

public interface ICanvasDataService
{
    Task InitializeAsync();
    Task<List<DbCourse>> GetCoursesAsync(bool forceSync = false, bool includeHidden = false);
    Task<List<DbAssignment>> GetAssignmentsAsync(long? courseId = null, bool forceSync = false);
    Task<DbAssignment?> GetAssignmentAsync(long assignmentId, bool forceSync = false);
    Task<List<DbSubmission>> GetSubmissionsAsync(long assignmentId, bool forceSync = false);
    
    Task SyncAllDataAsync();
    Task SyncCourseAssignmentsAsync(long courseId);
    Task SyncAssignmentSubmissionsAsync(long assignmentId);
    
    Task<string> DownloadSubmissionsAsync(long assignmentId, string baseDownloadPath);
    
    Task SaveAnalysisAsync(long assignmentId, string analysisJson);
    Task<string?> GetLatestAnalysisAsync(long assignmentId);
    
    Task SetCourseHiddenAsync(long courseId, bool isHidden);
    Task SetCourseCustomNameAsync(long courseId, string? customName);
}