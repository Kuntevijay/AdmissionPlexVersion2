using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Content;

public class Page : AuditableEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public PageType PageType { get; set; } = PageType.Static;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public long AuthorId { get; set; }
}
