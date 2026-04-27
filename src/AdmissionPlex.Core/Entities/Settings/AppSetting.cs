using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Settings;

/// <summary>
/// General key-value settings store grouped by category.
/// Categories: smtp, sms, whatsapp, push, google_auth, payment, general
/// Sensitive values (passwords, API keys) are stored encrypted.
/// </summary>
public class AppSetting : BaseEntity
{
    public string Category { get; set; } = string.Empty;   // e.g. "smtp", "google_auth"
    public string Key { get; set; } = string.Empty;        // e.g. "Host", "ClientId"
    public string Value { get; set; } = string.Empty;      // The setting value
    public bool IsSensitive { get; set; }                   // If true, value is encrypted at rest
    public bool IsEnabled { get; set; } = true;             // Quick toggle for the whole category
    public string? Description { get; set; }                // Admin-facing help text
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
}
