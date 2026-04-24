using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Content;

public class Faq : BaseEntity
{
    public string Category { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
