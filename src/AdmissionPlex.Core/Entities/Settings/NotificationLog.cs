using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Settings;

/// <summary>
/// Tracks every notification sent — for admin audit and debugging.
/// </summary>
public class NotificationLog : BaseEntity
{
    public long? UserId { get; set; }
    public string Channel { get; set; } = string.Empty;       // email, sms, whatsapp, push
    public string Recipient { get; set; } = string.Empty;      // email address, phone number, or device token
    public string? TemplateCode { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string Status { get; set; } = "Queued";             // Queued, Sent, Failed
    public string? ErrorMessage { get; set; }
    public string? ProviderResponse { get; set; }              // Raw response from SMTP/SMS/WhatsApp/FCM
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
