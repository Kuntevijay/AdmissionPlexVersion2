using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Cutoffs;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}
