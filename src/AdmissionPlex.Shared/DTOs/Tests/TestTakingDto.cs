namespace AdmissionPlex.Shared.DTOs.Tests;

/// <summary>
/// Question presented to student during test taking (no correct answers exposed)
/// </summary>
public class TestQuestionForStudentDto
{
    public long QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int QuestionNumber { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public List<OptionForStudentDto> Options { get; set; } = new();
}

public class OptionForStudentDto
{
    public long Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OptionOrder { get; set; }
}

/// <summary>
/// Response when starting a test
/// </summary>
public class StartTestResponseDto
{
    public Guid AttemptUuid { get; set; }
    public long AttemptId { get; set; }
    public string TestTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public string? Instructions { get; set; }
    public List<TestQuestionForStudentDto> Questions { get; set; } = new();
}

/// <summary>
/// Batch submit all answers at once
/// </summary>
public class FinishTestDto
{
    public List<SubmitAnswerDto> Answers { get; set; } = new();
}
