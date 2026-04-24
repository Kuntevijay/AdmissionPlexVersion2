using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Tests;

public class QuestionOption : BaseEntity
{
    public long QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OptionOrder { get; set; }
    public bool IsCorrect { get; set; }
    public decimal ScoreValue { get; set; }
    public StreamType? StreamTag { get; set; }

    // Navigation
    public Question Question { get; set; } = null!;
}
