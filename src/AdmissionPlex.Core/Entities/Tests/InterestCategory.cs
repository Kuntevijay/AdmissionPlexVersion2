using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Tests;

public class InterestCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
