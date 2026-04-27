namespace AdmissionPlex.Shared.DTOs.Tests;

public class QuestionCreateDto
{
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "Mcq";
    public string SectionType { get; set; } = "Interest";
    public long? InterestCategoryId { get; set; }
    public long? AptitudeCategoryId { get; set; }
    public string Difficulty { get; set; } = "Medium";
    public decimal Weightage { get; set; } = 1.00m;
    public decimal MaxScore { get; set; } = 1.00m;
    public string? Explanation { get; set; }
    public string? ImageUrl { get; set; }
    public List<OptionCreateDto> Options { get; set; } = new();
}

public class OptionCreateDto
{
    public string OptionText { get; set; } = string.Empty;
    public int OptionOrder { get; set; }
    public bool IsCorrect { get; set; }
    public decimal ScoreValue { get; set; }
    public string? StreamTag { get; set; }
    public string? ImageUrl { get; set; }
}
