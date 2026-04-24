namespace AdmissionPlex.Shared.DTOs.Tests;

public class TestDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string TestType { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public bool IncludesCounsellorSession { get; set; }
    public string? Instructions { get; set; }
    public List<TestSectionDto> Sections { get; set; } = new();
}

public class TestSectionDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SectionType { get; set; } = string.Empty;
    public int SectionOrder { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public string? InterestCategoryName { get; set; }
    public string? AptitudeCategoryName { get; set; }
    public int QuestionCount { get; set; }
}
