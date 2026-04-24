using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Tests;

public class AptitudeScore : BaseEntity
{
    public long AttemptId { get; set; }
    public long AptitudeCategoryId { get; set; }
    public decimal RawScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal PercentileScore { get; set; }
    public int RankOrder { get; set; }

    // Navigation
    public TestAttempt Attempt { get; set; } = null!;
    public AptitudeCategory AptitudeCategory { get; set; } = null!;
}
