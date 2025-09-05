using AiGrader.Models;

namespace AiGrader.Services;

public class CanvasDataService : ICanvasDataService
{
    private readonly ICanvasApiService _canvasApiService;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<CanvasDataService> _logger;
    
    public CanvasDataService(
        ICanvasApiService canvasApiService,
        IDatabaseService databaseService,
        ILogger<CanvasDataService> logger)
    {
        _canvasApiService = canvasApiService;
        _databaseService = databaseService;
        _logger = logger;
    }
    
    public async Task InitializeAsync()
    {
        await _databaseService.InitializeDatabaseAsync();
        
        if (await _databaseService.NeedsInitialSync())
        {
            _logger.LogInformation("Performing initial data sync from Canvas");
            await SyncAllDataAsync();
        }
    }
    
    public async Task<List<DbCourse>> GetCoursesAsync(bool forceSync = false, bool includeHidden = false)
    {
        if (forceSync || await ShouldSync())
        {
            await SyncCoursesAsync();
        }
        
        return await _databaseService.GetCoursesAsync(includeHidden);
    }
    
    public async Task<List<DbAssignment>> GetAssignmentsAsync(long? courseId = null, bool forceSync = false)
    {
        if (forceSync)
        {
            if (courseId.HasValue)
            {
                await SyncCourseAssignmentsAsync(courseId.Value);
            }
            else
            {
                var courses = await _databaseService.GetCoursesAsync();
                foreach (var course in courses)
                {
                    await SyncCourseAssignmentsAsync(course.Id);
                }
            }
        }
        
        return await _databaseService.GetAssignmentsAsync(courseId);
    }
    
    public async Task<DbAssignment?> GetAssignmentAsync(long assignmentId, bool forceSync = false)
    {
        var assignment = await _databaseService.GetAssignmentAsync(assignmentId);
        
        if (forceSync || assignment == null)
        {
            await SyncAssignmentSubmissionsAsync(assignmentId);
            assignment = await _databaseService.GetAssignmentAsync(assignmentId);
        }
        
        return assignment;
    }
    
    public async Task<List<DbSubmission>> GetSubmissionsAsync(long assignmentId, bool forceSync = false)
    {
        if (forceSync)
        {
            await SyncAssignmentSubmissionsAsync(assignmentId);
        }
        
        return await _databaseService.GetSubmissionsAsync(assignmentId);
    }
    
    public async Task SyncAllDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting full data sync");
            
            await SyncCoursesAsync();
            
            var courses = await _databaseService.GetCoursesAsync();
            foreach (var course in courses)
            {
                if (course.WorkflowState == "available")
                {
                    await SyncCourseAssignmentsAsync(course.Id);
                }
            }
            
            _logger.LogInformation("Full data sync completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full data sync");
            throw;
        }
    }
    
    public async Task SyncCourseAssignmentsAsync(long courseId)
    {
        try
        {
            var assignments = await _canvasApiService.GetCourseAssignmentsAsync(courseId);
            await _databaseService.SyncAssignmentsAsync(courseId, assignments);
            
            // Update assignment statistics
            foreach (var assignment in assignments)
            {
                try
                {
                    // Debug: Log assignment data being synced
                    _logger.LogInformation($"Syncing assignment: {assignment.Name}, Published: {assignment.Published}, DueAt: {assignment.DueAt}");
                    
                    var (total, ungraded) = await _canvasApiService.GetAssignmentSubmissionStatsAsync(courseId, assignment.Id);
                    await _databaseService.UpdateAssignmentStatsAsync(assignment.Id, total, ungraded);
                    
                    _logger.LogInformation($"Updated stats for {assignment.Name}: {total} total, {ungraded} ungraded");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to update stats for assignment {assignment.Id}");
                }
            }
            
            _logger.LogInformation($"Synced {assignments.Count} assignments for course {courseId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing assignments for course {courseId}");
            throw;
        }
    }
    
    public async Task SyncAssignmentSubmissionsAsync(long assignmentId)
    {
        try
        {
            var assignment = await _databaseService.GetAssignmentAsync(assignmentId);
            if (assignment == null)
            {
                _logger.LogWarning($"Assignment {assignmentId} not found in database");
                return;
            }
            
            var submissions = await _canvasApiService.GetSubmissionsAsync(assignment.CourseId, assignmentId);
            var userIds = submissions.Select(s => s.UserId).Distinct().ToList();
            var users = await _canvasApiService.GetUsersAsync(assignment.CourseId, userIds);
            
            await _databaseService.SyncSubmissionsAsync(assignmentId, submissions, users);
            _logger.LogInformation($"Synced {submissions.Count} submissions for assignment {assignmentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error syncing submissions for assignment {assignmentId}");
            throw;
        }
    }
    
    public async Task<string> DownloadSubmissionsAsync(long assignmentId, string baseDownloadPath)
    {
        _logger.LogInformation($"Starting download for assignment {assignmentId} to {baseDownloadPath}");
        
        var assignment = await _databaseService.GetAssignmentAsync(assignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment {assignmentId} not found");
        }
        
        _logger.LogInformation($"Found assignment: {assignment.Name} in course {assignment.Course.Name}");
        
        var assignmentPath = Path.Combine(baseDownloadPath, 
            SanitizeFileName($"{assignment.Course.CourseCode}_{assignment.Course.Name}"),
            SanitizeFileName(assignment.Name));
        
        _logger.LogInformation($"Creating directory: {assignmentPath}");
        Directory.CreateDirectory(assignmentPath);
        
        var submissions = await _databaseService.GetSubmissionsAsync(assignmentId);
        _logger.LogInformation($"Found {submissions.Count} submissions to download");
        
        // If no submissions exist in database, try to sync them first
        if (!submissions.Any())
        {
            _logger.LogInformation("No submissions in database, attempting to sync from Canvas");
            await SyncAssignmentSubmissionsAsync(assignmentId);
            submissions = await _databaseService.GetSubmissionsAsync(assignmentId);
            _logger.LogInformation($"After sync: {submissions.Count} submissions found");
        }
        
        foreach (var submission in submissions)
        {
            var studentPath = Path.Combine(assignmentPath, SanitizeFileName(submission.StudentName));
            Directory.CreateDirectory(studentPath);
            
            if (!string.IsNullOrEmpty(submission.Body))
            {
                var textFilePath = Path.Combine(studentPath, "submission.txt");
                await File.WriteAllTextAsync(textFilePath, submission.Body);
                await _databaseService.UpdateSubmissionLocalPathAsync(submission.Id, textFilePath);
                _logger.LogInformation($"Saved text submission for {submission.StudentName}");
            }
            
            foreach (var attachment in submission.Attachments)
            {
                try
                {
                    var content = await _canvasApiService.DownloadAttachmentAsync(attachment.Url);
                    var filePath = Path.Combine(studentPath, SanitizeFileName(attachment.Filename));
                    await File.WriteAllTextAsync(filePath, content);
                    await _databaseService.UpdateAttachmentDownloadStatusAsync(attachment.Id, filePath);
                    _logger.LogInformation($"Downloaded attachment {attachment.Filename} for {submission.StudentName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to download attachment {attachment.Id}");
                }
            }
        }
        
        await _databaseService.UpdateAssignmentSubmissionsDownloadedAsync(assignmentId, assignmentPath);
        _logger.LogInformation($"Download completed. Files saved to: {assignmentPath}");
        
        return assignmentPath;
    }
    
    public async Task SaveAnalysisAsync(long assignmentId, string analysisJson)
    {
        await _databaseService.SaveAnalysisResultAsync(assignmentId, analysisJson);
    }
    
    public async Task<string?> GetLatestAnalysisAsync(long assignmentId)
    {
        return await _databaseService.GetLatestAnalysisResultAsync(assignmentId);
    }
    
    public async Task SetCourseHiddenAsync(long courseId, bool isHidden)
    {
        await _databaseService.SetCourseHiddenStatusAsync(courseId, isHidden);
    }
    
    public async Task SetCourseCustomNameAsync(long courseId, string? customName)
    {
        await _databaseService.SetCourseCustomNameAsync(courseId, customName);
    }
    
    private async Task SyncCoursesAsync()
    {
        try
        {
            var courses = await _canvasApiService.GetCurrentCoursesAsync();
            await _databaseService.SyncCoursesAsync(courses);
            _logger.LogInformation($"Synced {courses.Count} courses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing courses");
            throw;
        }
    }
    
    private async Task<bool> ShouldSync()
    {
        var lastSync = await _databaseService.GetLastSyncTimeAsync();
        return lastSync == null || DateTime.UtcNow - lastSync > TimeSpan.FromHours(1);
    }
    
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized.Substring(0, 100) : sanitized;
    }
}