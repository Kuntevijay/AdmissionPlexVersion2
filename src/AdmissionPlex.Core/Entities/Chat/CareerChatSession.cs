using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Core.Entities.Chat;

public class CareerChatSession : AuditableEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long StudentId { get; set; }
    public string? Title { get; set; }
    public string? ContextJson { get; set; }
    public bool IsActive { get; set; } = true;

    public StudentProfile Student { get; set; } = null!;
    public ICollection<CareerChatMessage> Messages { get; set; } = new List<CareerChatMessage>();
}
