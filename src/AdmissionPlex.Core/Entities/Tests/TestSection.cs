using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Tests;

public class TestSection : BaseEntity
{
    public long TestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public SectionType SectionType { get; set; }
    public int SectionOrder { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public long? InterestCategoryId { get; set; }
    public long? AptitudeCategoryId { get; set; }

    // Navigation
    public Test Test { get; set; } = null!;
    public InterestCategory? InterestCategory { get; set; }
    public AptitudeCategory? AptitudeCategory { get; set; }
    public ICollection<TestSectionQuestion> Questions { get; set; } = new List<TestSectionQuestion>();
}
