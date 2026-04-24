using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Chat;

public class CareerChatMessage : BaseEntity
{
    public long SessionId { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CareerChatSession Session { get; set; } = null!;
}
