using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Careers;

public class CareerSubject : BaseEntity
{
    public long CareerId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public SubjectImportance Importance { get; set; } = SubjectImportance.Recommended;

    public Career Career { get; set; } = null!;
}
