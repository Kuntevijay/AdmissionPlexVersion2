using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;


namespace AdmissionPlex.Core.Entities.Users;

public class StudentProfile : AuditableEntity
{
    public long UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? CurrentClass { get; set; }
    public string? SchoolName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public BoardType? Board { get; set; }
    public StreamType Stream { get; set; } = StreamType.Undecided;
    public string? ParentPhone { get; set; }
    public string? ParentEmail { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ReferredByCode { get; set; }
    public long? CoordinatorId { get; set; }

    // Navigation
    public CoordinatorProfile? Coordinator { get; set; }
}
