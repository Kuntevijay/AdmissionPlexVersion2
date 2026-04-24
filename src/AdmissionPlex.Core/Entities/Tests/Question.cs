using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Tests;

public class Question : AuditableEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public SectionType SectionType { get; set; }
    public long? InterestCategoryId { get; set; }
    public long? AptitudeCategoryId { get; set; }
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Medium;
    public decimal Weightage { get; set; } = 1.00m;
    public decimal MaxScore { get; set; } = 1.00m;
    public string? Explanation { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public long CreatedBy { get; set; }

    // Navigation
    public InterestCategory? InterestCategory { get; set; }
    public AptitudeCategory? AptitudeCategory { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}
