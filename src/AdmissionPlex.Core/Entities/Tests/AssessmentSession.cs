using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Core.Entities.Tests;

/// <summary>
/// Groups all 9 sub-test attempts for a student's psychometric assessment.
/// Only when all sub-tests are completed can the report be generated.
/// </summary>
public class AssessmentSession : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long StudentId { get; set; }
    public bool AllCompleted { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; } = 9;
    public Guid? SavedReportId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public StudentProfile Student { get; set; } = null!;
    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
    // Scores stored at session level after all sub-tests complete
    public ICollection<InterestScore> InterestScores { get; set; } = new List<InterestScore>();
    public ICollection<AptitudeScore> AptitudeScores { get; set; } = new List<AptitudeScore>();
    public ICollection<CareerSuitabilityScore> CareerSuitabilityScores { get; set; } = new List<CareerSuitabilityScore>();
    public int? OverallIqScore { get; set; }
    public string? IqCategory { get; set; }
}
