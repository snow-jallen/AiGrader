using System.Text;
using System.Text.Json;
using AiGrader.Models;

namespace AiGrader.Services;

public class AiAnalysisService : IAiAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _modelUrl;
    private readonly string _modelName;

    public AiAnalysisService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _modelUrl = _configuration["AiModel:Url"] ?? throw new InvalidOperationException("AI model URL not configured");
        _modelName = _configuration["AiModel:Name"] ?? throw new InvalidOperationException("AI model name not configured");
    }

    public async Task<OverallAnalysis> AnalyzeSubmissionsAsync(List<SubmissionAnalysis> submissions, string assignmentName)
    {
        var analysis = new OverallAnalysis();
        
        // Calculate statistics
        analysis.Statistics = CalculateStatistics(submissions);
        
        // Analyze each submission individually
        var analyzedSubmissions = new List<SubmissionAnalysis>();
        
        foreach (var submission in submissions)
        {
            var analyzedSubmission = await AnalyzeIndividualSubmissionAsync(submission, assignmentName);
            analyzedSubmissions.Add(analyzedSubmission);
        }
        
        // Find standout submissions
        analysis.StandoutSubmissions = FindStandoutSubmissions(analyzedSubmissions, analysis.Statistics);
        
        // Find suspicious similarities
        analysis.SuspiciousSimilarities = await FindSimilaritiesAsync(analyzedSubmissions);
        
        // Generate overall summary
        analysis.Summary = await GenerateOverallSummaryAsync(analyzedSubmissions, assignmentName, analysis.Statistics);
        
        return analysis;
    }

    private async Task<SubmissionAnalysis> AnalyzeIndividualSubmissionAsync(SubmissionAnalysis submission, string assignmentName)
    {
        var prompt = $@"Analyze this student submission for assignment '{assignmentName}'. 
        
Student: {submission.StudentName}
Submission: {submission.SubmissionContent}

Please provide:
1. A brief analysis of the quality and content
2. Any notable strengths or weaknesses
3. Overall assessment

Keep the analysis concise and constructive.";

        var analysis = await CallAiModelAsync(prompt);
        submission.Analysis = analysis;
        submission.WordCount = CountWords(submission.SubmissionContent);
        
        return submission;
    }

    private List<SubmissionAnalysis> FindStandoutSubmissions(List<SubmissionAnalysis> submissions, AnalysisStatistics stats)
    {
        var standouts = new List<SubmissionAnalysis>();
        
        foreach (var submission in submissions)
        {
            var reasons = new List<string>();
            
            // Check for extremely short submissions (less than 25% of average)
            if (submission.WordCount < stats.AverageWordCount * 0.25 && submission.WordCount > 0)
            {
                reasons.Add("Extremely short response");
            }
            
            // Check for extremely long submissions (more than 200% of average)
            if (submission.WordCount > stats.AverageWordCount * 2)
            {
                reasons.Add("Exceptionally detailed response");
            }
            
            // Check if it's the shortest or longest
            if (submission.WordCount == stats.ShortestSubmission && stats.ShortestSubmission > 0)
            {
                reasons.Add("Shortest submission");
            }
            
            if (submission.WordCount == stats.LongestSubmission)
            {
                reasons.Add("Longest submission");
            }
            
            if (reasons.Any())
            {
                submission.IsStandout = true;
                submission.StandoutReason = string.Join(", ", reasons);
                standouts.Add(submission);
            }
        }
        
        return standouts;
    }

    private async Task<List<SimilarityGroup>> FindSimilaritiesAsync(List<SubmissionAnalysis> submissions)
    {
        var similarities = new List<SimilarityGroup>();
        var processed = new HashSet<int>();
        
        for (int i = 0; i < submissions.Count; i++)
        {
            if (processed.Contains(i)) continue;
            
            var similarGroup = new List<string> { submissions[i].StudentName };
            var baseSubmission = submissions[i].SubmissionContent;
            
            for (int j = i + 1; j < submissions.Count; j++)
            {
                if (processed.Contains(j)) continue;
                
                var similarity = await CalculateSimilarityAsync(baseSubmission, submissions[j].SubmissionContent);
                
                if (similarity > 0.8) // High similarity threshold
                {
                    similarGroup.Add(submissions[j].StudentName);
                    processed.Add(j);
                }
            }
            
            if (similarGroup.Count > 1)
            {
                similarities.Add(new SimilarityGroup
                {
                    StudentNames = similarGroup,
                    SimilarityScore = 0.8, // Simplified - in reality would calculate actual score
                    Reason = "High text similarity detected"
                });
                processed.Add(i);
            }
        }
        
        return similarities;
    }

    private async Task<double> CalculateSimilarityAsync(string text1, string text2)
    {
        // Simple similarity check - in a real implementation, you might use more sophisticated NLP
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0;
            
        var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union == 0 ? 0 : (double)intersection / union;
    }

    private async Task<string> GenerateOverallSummaryAsync(List<SubmissionAnalysis> submissions, string assignmentName, AnalysisStatistics stats)
    {
        var submissionSummaries = submissions.Take(5).Select(s => $"- {s.StudentName}: {s.Analysis.Split('.')[0]}").ToList();
        
        var prompt = $@"Generate an overall summary for assignment '{assignmentName}' based on {stats.TotalSubmissions} submissions.

Key Statistics:
- Total submissions: {stats.TotalSubmissions}
- Average word count: {stats.AverageWordCount:F0}
- Range: {stats.ShortestSubmission} to {stats.LongestSubmission} words

Sample submission analyses:
{string.Join("\n", submissionSummaries)}

Please provide:
1. Overall quality assessment of the submissions
2. Common themes or patterns observed
3. General recommendations for the instructor

Keep it concise and professional.";

        return await CallAiModelAsync(prompt);
    }

    private AnalysisStatistics CalculateStatistics(List<SubmissionAnalysis> submissions)
    {
        if (!submissions.Any())
            return new AnalysisStatistics();
            
        var wordCounts = submissions.Select(s => CountWords(s.SubmissionContent)).ToList();
        
        return new AnalysisStatistics
        {
            TotalSubmissions = submissions.Count,
            AverageWordCount = wordCounts.Average(),
            ShortestSubmission = wordCounts.Min(),
            LongestSubmission = wordCounts.Max()
        };
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
            
        return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private async Task<string> CallAiModelAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = _modelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Add API key header (assuming OpenAI format)
            var apiKey = _configuration["OpenAiApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await _httpClient.PostAsync(_modelUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                return $"Error calling AI model: {response.StatusCode}";
            }
            
            var responseJson = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseJson);
            
            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No response from AI model";
        }
        catch (Exception ex)
        {
            return $"Error analyzing submission: {ex.Message}";
        }
    }
}