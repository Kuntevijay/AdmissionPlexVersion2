using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Tests;

public class Test : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TestType TestType { get; set; }
    public string? Category { get; set; } // "Interest", "Aptitude", "IQ"
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IncludesCounsellorSession { get; set; }
    public string? Instructions { get; set; }

    // ── Access & flow configuration ──
    public bool IsPublic { get; set; }
    public bool RequiresPayment { get; set; }
    public bool IsContinuityFlow { get; set; }
    public string? ParentTestCode { get; set; }

    // Navigation
    public ICollection<TestSection> Sections { get; set; } = new List<TestSection>();
}
