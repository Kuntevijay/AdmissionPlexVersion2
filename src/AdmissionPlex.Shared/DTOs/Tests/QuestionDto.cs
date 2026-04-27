namespace AdmissionPlex.Shared.DTOs.Tests;

public class QuestionDto
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string SectionType { get; set; } = string.Empty;
    public long? InterestCategoryId { get; set; }
    public string? InterestCategoryName { get; set; }
    public long? AptitudeCategoryId { get; set; }
    public string? AptitudeCategoryName { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public decimal Weightage { get; set; }
    public decimal MaxScore { get; set; }
    public string? Explanation { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
}

public class QuestionOptionDto
{
    public long Id { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int OptionOrder { get; set; }
    public bool IsCorrect { get; set; }
    public decimal ScoreValue { get; set; }
    public string? StreamTag { get; set; }
    public string? ImageUrl { get; set; }
}
