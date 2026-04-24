using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Tests;

public class TestResponse : BaseEntity
{
    public long AttemptId { get; set; }
    public long QuestionId { get; set; }
    public long? SelectedOptionId { get; set; }
    public string? OpenAnswer { get; set; }
    public decimal ScoreObtained { get; set; }
    public int? TimeTakenSeconds { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TestAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public QuestionOption? SelectedOption { get; set; }
}
