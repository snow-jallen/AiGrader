using System.Text.Json.Serialization;

namespace AiGrader.Models;

public class Assignment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("course_id")]
    public long CourseId { get; set; }
    
    [JsonPropertyName("due_at")]
    public DateTime? DueAt { get; set; }
    
    [JsonPropertyName("published")]
    public bool Published { get; set; }
    
    [JsonPropertyName("points_possible")]
    public double? PointsPossible { get; set; }
}

public class Submission
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public long UserId { get; set; }
    
    [JsonPropertyName("assignment_id")]
    public long AssignmentId { get; set; }
    
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    
    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }
    
    [JsonPropertyName("workflow_state")]
    public string WorkflowState { get; set; } = string.Empty;
    
    [JsonPropertyName("grade")]
    public string? Grade { get; set; }
    
    [JsonPropertyName("attachments")]
    public List<Attachment>? Attachments { get; set; }
}

public class Attachment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("content-type")]
    public string ContentType { get; set; } = string.Empty;
}

public class User
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("sortable_name")]
    public string SortableName { get; set; } = string.Empty;
}

public class Course
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("course_code")]
    public string CourseCode { get; set; } = string.Empty;
    
    [JsonPropertyName("workflow_state")]
    public string WorkflowState { get; set; } = string.Empty;
}