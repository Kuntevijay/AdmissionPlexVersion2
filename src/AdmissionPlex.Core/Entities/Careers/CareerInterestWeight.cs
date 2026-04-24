using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Entities.Tests;

namespace AdmissionPlex.Core.Entities.Careers;

public class CareerInterestWeight : BaseEntity
{
    public long CareerId { get; set; }
    public long InterestCategoryId { get; set; }
    public decimal Weight { get; set; }
    public decimal MinPercentile { get; set; }

    public Career Career { get; set; } = null!;
    public InterestCategory InterestCategory { get; set; } = null!;
}
