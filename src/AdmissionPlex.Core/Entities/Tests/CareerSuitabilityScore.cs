using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Entities.Careers;

namespace AdmissionPlex.Core.Entities.Tests;

public class CareerSuitabilityScore : BaseEntity
{
    public long AttemptId { get; set; }
    public long CareerId { get; set; }
    public decimal SuitabilityPct { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsCanBeConsidered { get; set; }
    public int RankOrder { get; set; }

    // Navigation
    public TestAttempt Attempt { get; set; } = null!;
    public Career Career { get; set; } = null!;
}
