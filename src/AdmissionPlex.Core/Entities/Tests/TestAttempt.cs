using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Core.Entities.Tests;

public class TestAttempt : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long StudentId { get; set; }
    public long TestId { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.Started;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public StreamType? RecommendedStream { get; set; }
    public int? OverallIqScore { get; set; }
    public IqCategory? IqCategory { get; set; }
    public string? PdfReportUrl { get; set; }
    public long? PaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public StudentProfile Student { get; set; } = null!;
    public Test Test { get; set; } = null!;
    public ICollection<TestResponse> Responses { get; set; } = new List<TestResponse>();
    public ICollection<InterestScore> InterestScores { get; set; } = new List<InterestScore>();
    public ICollection<AptitudeScore> AptitudeScores { get; set; } = new List<AptitudeScore>();
    public ICollection<CareerSuitabilityScore> CareerSuitabilityScores { get; set; } = new List<CareerSuitabilityScore>();
}
