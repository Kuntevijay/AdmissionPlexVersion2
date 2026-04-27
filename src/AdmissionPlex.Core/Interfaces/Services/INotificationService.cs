namespace AdmissionPlex.Core.Interfaces.Services;

/// <summary>
/// Unified notification orchestrator. Dispatches to the appropriate channel
/// (email, SMS, WhatsApp, push) based on template config and provider settings.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send a notification using a template code. Replaces placeholders with data values.
    /// Returns true if at least one channel succeeded.
    /// </summary>
    Task<bool> SendByTemplateAsync(string templateCode, long userId, Dictionary<string, string> data);

    /// <summary>Send an email directly (bypasses templates).</summary>
    Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? fromName = null);

    /// <summary>Send an SMS directly.</summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message);

    /// <summary>Send a WhatsApp message directly.</summary>
    Task<bool> SendWhatsAppAsync(string phoneNumber, string message, string? templateName = null, Dictionary<string, string>? templateParams = null);

    /// <summary>Send a push notification directly.</summary>
    Task<bool> SendPushAsync(string deviceToken, string title, string body, string? actionUrl = null, string? imageUrl = null);

    /// <summary>Send a push notification to a user (by userId, looks up device token).</summary>
    Task<bool> SendPushToUserAsync(long userId, string title, string body, string? actionUrl = null);

    /// <summary>Send a test notification for a specific channel (for admin testing).</summary>
    Task<(bool Success, string Message)> SendTestAsync(string channel, string recipient);
}
