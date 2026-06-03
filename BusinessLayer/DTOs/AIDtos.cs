#nullable enable

namespace BusinessLayer.DTOs
{
    // ─── Configuration ────────────────────────────────────────────────────────

    public class AIOptions
    {
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "claude-haiku-4-5-20251001";
        public int MaxTokens { get; set; } = 1024;
        public int TimeoutSeconds { get; set; } = 30;
    }

    // ─── Feature 1: AI Chat ───────────────────────────────────────────────────

    public class AIChatRequestDto
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }

    public class AIChatRelatedDataDto
    {
        public int TotalStudents { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public double AverageScore { get; set; }
    }

    public class AIChatResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public AIChatRelatedDataDto? RelatedData { get; set; }
    }

    // ─── Feature 2: Statistics ────────────────────────────────────────────────

    public class ComponentStatDto
    {
        public string Name { get; set; } = string.Empty;
        public double Average { get; set; }
        public int ZeroCount { get; set; }
        public int EmptyCount { get; set; }
    }

    public class AIStatisticsResponseDto
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public double PassRate { get; set; }
        public double AverageScore { get; set; }
        public List<ComponentStatDto> ComponentStats { get; set; } = new();
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
        public List<string> AiInsights { get; set; } = new();
    }

    // ─── Feature 3: Anomaly Detection ────────────────────────────────────────

    public class AIAnomalyDto
    {
        public string RollNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class AIAnomaliesResponseDto
    {
        public List<AIAnomalyDto> Anomalies { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    // ─── Feature 4 (removed): Risk Prediction ────────────────────────────────

    // ─── Feature 5: Comment Suggestions ──────────────────────────────────────

    public class AISuggestCommentDto
    {
        public string RollNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SuggestedComment { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
    }

    public class AISuggestCommentsResponseDto
    {
        public List<AISuggestCommentDto> Suggestions { get; set; } = new();
    }
}
