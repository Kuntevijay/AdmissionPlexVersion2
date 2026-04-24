using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Users;

public class CounsellorProfile : AuditableEntity
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public int ExperienceYears { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsAvailable { get; set; } = true;
    public decimal Rating { get; set; }
    public int TotalSessions { get; set; }
    public string? AvatarUrl { get; set; }

    // Navigation
}
