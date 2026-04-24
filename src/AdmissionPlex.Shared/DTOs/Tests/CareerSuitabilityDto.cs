namespace AdmissionPlex.Shared.DTOs.Tests;

public class CareerSuitabilityDto
{
    public long CareerId { get; set; }
    public string CareerTitle { get; set; } = string.Empty;
    public string StreamName { get; set; } = string.Empty;
    public decimal SuitabilityPct { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsCanBeConsidered { get; set; }
    public int RankOrder { get; set; }
    public string? EducationPath { get; set; }
    public string? EducationCostRange { get; set; }
    public string? AdmissionInfo { get; set; }
}
