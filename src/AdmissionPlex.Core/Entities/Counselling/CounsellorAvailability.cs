using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Core.Entities.Counselling;

public class CounsellorAvailability : BaseEntity
{
    public long CounsellorId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;

    public CounsellorProfile Counsellor { get; set; } = null!;
}
