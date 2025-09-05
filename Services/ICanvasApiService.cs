using AiGrader.Models;

namespace AiGrader.Services;

public interface ICanvasApiService
{
    Task<Assignment?> GetAssignmentFromUrlAsync(string assignmentUrl);
    Task<List<Submission>> GetSubmissionsAsync(long courseId, long assignmentId);
    Task<Dictionary<long, User>> GetUsersAsync(long courseId, List<long> userIds);
    Task<string> DownloadAttachmentAsync(string url);
    Task<List<Course>> GetCurrentCoursesAsync();
    Task<List<Assignment>> GetCourseAssignmentsAsync(long courseId);
    Task<(int total, int ungraded)> GetAssignmentSubmissionStatsAsync(long courseId, long assignmentId);
}