using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Settings;

/// <summary>
/// Notification templates with placeholder support.
/// Placeholders: {{StudentName}}, {{TestName}}, {{ReportUrl}}, {{OTP}}, {{Amount}}, {{OrderId}}, etc.
/// </summary>
public class NotificationTemplate : AuditableEntity
{
    public string Code { get; set; } = string.Empty;         // e.g. "welcome", "test_complete", "payment_success"
    public string Name { get; set; } = string.Empty;         // Friendly display name
    public string Channel { get; set; } = "email";           // email, sms, whatsapp, push

    // Email fields
    public string? Subject { get; set; }
    public string? BodyHtml { get; set; }

    // SMS / WhatsApp / Push fields
    public string? BodyText { get; set; }

    // WhatsApp template fields (for approved templates)
    public string? WhatsAppTemplateName { get; set; }

    // Push notification fields
    public string? PushTitle { get; set; }
    public string? PushImageUrl { get; set; }
    public string? ActionUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
