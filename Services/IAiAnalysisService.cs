using AiGrader.Models;

namespace AiGrader.Services;

public interface IAiAnalysisService
{
    Task<OverallAnalysis> AnalyzeSubmissionsAsync(List<SubmissionAnalysis> submissions, string assignmentName);
}