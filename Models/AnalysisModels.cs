namespace AiGrader.Models;

public class SubmissionAnalysis
{
    public string StudentName { get; set; } = string.Empty;
    public string SubmissionContent { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public bool IsStandout { get; set; }
    public string StandoutReason { get; set; } = string.Empty;
    public List<SimilarityMatch> SimilarMatches { get; set; } = new();
}

public class SimilarityMatch
{
    public string StudentName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class OverallAnalysis
{
    public string Summary { get; set; } = string.Empty;
    public List<SubmissionAnalysis> StandoutSubmissions { get; set; } = new();
    public List<SimilarityGroup> SuspiciousSimilarities { get; set; } = new();
    public AnalysisStatistics Statistics { get; set; } = new();
}

public class SimilarityGroup
{
    public List<string> StudentNames { get; set; } = new();
    public double SimilarityScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AnalysisStatistics
{
    public int TotalSubmissions { get; set; }
    public double AverageWordCount { get; set; }
    public int ShortestSubmission { get; set; }
    public int LongestSubmission { get; set; }
}