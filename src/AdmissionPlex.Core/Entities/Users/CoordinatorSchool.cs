using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Users;

public class CoordinatorSchool : BaseEntity
{
    public long CoordinatorId { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string SchoolCity { get; set; } = string.Empty;
    public string? SchoolBoard { get; set; }
    public int StudentCount { get; set; }
    public bool AgreementSigned { get; set; }

    // Navigation
    public CoordinatorProfile Coordinator { get; set; } = null!;
}
