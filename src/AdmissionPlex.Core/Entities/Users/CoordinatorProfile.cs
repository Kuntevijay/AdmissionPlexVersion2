using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Users;

public class CoordinatorProfile : AuditableEntity
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public decimal CommissionPct { get; set; } = 10.00m;
    public int TotalReferrals { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<CoordinatorSchool> Schools { get; set; } = new List<CoordinatorSchool>();
}
