namespace AdmissionPlex.Shared.DTOs.Counselling;

public class CounsellorDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Qualification { get; set; } = "";
    public string? Specialization { get; set; }
    public string? Bio { get; set; }
    public int ExperienceYears { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal Rating { get; set; }
    public int TotalSessions { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsAvailable { get; set; }
}

public class AvailabilityDto
{
    public long Id { get; set; }
    public int DayOfWeek { get; set; }
    public string DayName => DayOfWeek switch { 0 => "Sunday", 1 => "Monday", 2 => "Tuesday", 3 => "Wednesday", 4 => "Thursday", 5 => "Friday", 6 => "Saturday", _ => "" };
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
}

public class SessionDto
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }
    public string StudentName { get; set; } = "";
    public string CounsellorName { get; set; } = "";
    public string SessionType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? MeetingLink { get; set; }
    public string? Notes { get; set; }
    public string? StudentFeedback { get; set; }
    public int? Rating { get; set; }
}

public class BookSessionRequest
{
    public long CounsellorId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string SessionType { get; set; } = "Video";
    public long? TestAttemptId { get; set; }
}
