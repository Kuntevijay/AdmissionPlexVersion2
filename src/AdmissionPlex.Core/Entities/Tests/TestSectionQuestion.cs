using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Tests;

public class TestSectionQuestion : BaseEntity
{
    public long SectionId { get; set; }
    public long QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public int? TimeLimitSeconds { get; set; }

    // Navigation
    public TestSection Section { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
