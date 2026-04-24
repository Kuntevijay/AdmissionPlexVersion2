using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Careers;

public class Career : AuditableEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long StreamId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? EducationPath { get; set; }
    public string? EducationCostRange { get; set; }
    public string? AdmissionInfo { get; set; }
    public decimal? AvgSalaryMin { get; set; }
    public decimal? AvgSalaryMax { get; set; }
    public GrowthOutlook GrowthOutlook { get; set; } = GrowthOutlook.Medium;
    public string? JobMarketSize { get; set; }
    public string? SkillsRequired { get; set; }       // JSON array
    public string? TopColleges { get; set; }           // JSON array
    public string? EntranceExams { get; set; }         // JSON array
    public decimal SuitabilityCutoffPct { get; set; } = 80.00m;
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;

    // Navigation
    public CareerStream Stream { get; set; } = null!;
    public ICollection<CareerSubject> Subjects { get; set; } = new List<CareerSubject>();
    public ICollection<CareerInterestWeight> InterestWeights { get; set; } = new List<CareerInterestWeight>();
    public ICollection<CareerAptitudeWeight> AptitudeWeights { get; set; } = new List<CareerAptitudeWeight>();
}
