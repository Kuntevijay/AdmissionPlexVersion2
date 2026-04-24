namespace AdmissionPlex.Shared.DTOs.Tests;

public class TestAttemptDto
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }
    public long TestId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RecommendedStream { get; set; }
    public int? OverallIqScore { get; set; }
    public string? IqCategory { get; set; }
    public string? PdfReportUrl { get; set; }
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
}
