using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Careers;

public class CareerStream : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }

    public ICollection<Career> Careers { get; set; } = new List<Career>();
}
