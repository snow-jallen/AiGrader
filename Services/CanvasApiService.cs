using System.Text.Json;
using System.Text.RegularExpressions;
using AiGrader.Models;

namespace AiGrader.Services;

public class CanvasApiService : ICanvasApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _apiToken;

    public CanvasApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _apiToken = _configuration["CanvasApiToken"] ?? throw new InvalidOperationException("Canvas API token not configured");
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
    }

    public async Task<Assignment?> GetAssignmentFromUrlAsync(string assignmentUrl)
    {
        // Parse the URL to extract course ID and assignment ID
        // Expected format: https://snow.instructure.com/courses/{courseId}/assignments/{assignmentId}
        var regex = new Regex(@"courses/(\d+)/assignments/(\d+)");
        var match = regex.Match(assignmentUrl);
        
        if (!match.Success)
            throw new ArgumentException("Invalid Canvas assignment URL format");

        var courseId = long.Parse(match.Groups[1].Value);
        var assignmentId = long.Parse(match.Groups[2].Value);

        var response = await _httpClient.GetAsync($"https://snow.instructure.com/api/v1/courses/{courseId}/assignments/{assignmentId}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var assignment = JsonSerializer.Deserialize<Assignment>(json);
        
        return assignment;
    }

    public async Task<List<Submission>> GetSubmissionsAsync(long courseId, long assignmentId)
    {
        var submissions = new List<Submission>();
        var page = 1;
        const int perPage = 100;

        while (true)
        {
            var response = await _httpClient.GetAsync(
                $"https://snow.instructure.com/api/v1/courses/{courseId}/assignments/{assignmentId}/submissions?include[]=attachments&per_page={perPage}&page={page}");
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var pageSubmissions = JsonSerializer.Deserialize<List<Submission>>(json) ?? new List<Submission>();

            if (!pageSubmissions.Any())
                break;

            submissions.AddRange(pageSubmissions.Where(s => s.WorkflowState == "submitted" || s.WorkflowState == "graded"));
            page++;
        }

        return submissions;
    }

    public async Task<Dictionary<long, User>> GetUsersAsync(long courseId, List<long> userIds)
    {
        var users = new Dictionary<long, User>();
        
        // Canvas API allows up to 100 user IDs per request
        var chunks = userIds.Chunk(100);
        
        foreach (var chunk in chunks)
        {
            var userIdParams = string.Join("&", chunk.Select(id => $"user_ids[]={id}"));
            var response = await _httpClient.GetAsync($"https://snow.instructure.com/api/v1/courses/{courseId}/users?{userIdParams}");
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var chunkUsers = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            
            foreach (var user in chunkUsers)
            {
                users[user.Id] = user;
            }
        }

        return users;
    }

    public async Task<string> DownloadAttachmentAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}