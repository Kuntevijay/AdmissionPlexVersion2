using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Tests;

public class InterestScore : BaseEntity
{
    public long AttemptId { get; set; }
    public long InterestCategoryId { get; set; }
    public decimal RawScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal PercentileScore { get; set; }
    public int RankOrder { get; set; }

    // Navigation
    public TestAttempt Attempt { get; set; } = null!;
    public InterestCategory InterestCategory { get; set; } = null!;
}
