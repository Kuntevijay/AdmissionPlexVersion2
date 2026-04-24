using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Core.Entities.Counselling;

public class CounsellorSession : BaseEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long StudentId { get; set; }
    public long CounsellorId { get; set; }
    public long? TestAttemptId { get; set; }
    public SessionType SessionType { get; set; } = SessionType.Video;
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }
    public string? StudentFeedback { get; set; }
    public int? Rating { get; set; }
    public long? PaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentProfile Student { get; set; } = null!;
    public CounsellorProfile Counsellor { get; set; } = null!;
}
