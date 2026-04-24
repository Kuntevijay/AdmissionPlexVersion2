namespace AdmissionPlex.Shared.DTOs.Tests;

/// <summary>
/// Full 7-section psychometric report data matching the reference PDF format.
/// </summary>
public class PsychometricReportDto
{
    // Section 1: Report Summary
    public string StudentName { get; set; } = string.Empty;
    public string? StudentClass { get; set; }
    public string? SchoolName { get; set; }
    public DateTime TestDate { get; set; }
    public List<InterestScoreDto> Top3Interests { get; set; } = new();
    public List<AptitudeScoreDto> Top3Aptitudes { get; set; } = new();
    public int OverallIqScore { get; set; }
    public string IqCategory { get; set; } = string.Empty;
    public List<CareerSuitabilityDto> Top5Careers { get; set; } = new();

    // Section 2: Interest Profile (all 10 categories with percentile bar chart data)
    public List<InterestScoreDto> AllInterestScores { get; set; } = new();

    // Section 3: Aptitude Profile (all 7 categories with percentile bar chart data)
    public List<AptitudeScoreDto> AllAptitudeScores { get; set; } = new();

    // Section 4: IQ Benchmark
    // (uses OverallIqScore + IqCategory from Summary)

    // Section 5: Career Suitability (all careers with percentage match)
    public List<CareerSuitabilityDto> AllCareerSuitability { get; set; } = new();

    // Section 6: Recommended Careers (>= cutoff)
    public List<CareerSuitabilityDto> RecommendedCareers { get; set; } = new();

    // Section 7: Careers That Can Be Considered
    public List<CareerSuitabilityDto> CareersToConsider { get; set; } = new();

    public string? PdfReportUrl { get; set; }
}
