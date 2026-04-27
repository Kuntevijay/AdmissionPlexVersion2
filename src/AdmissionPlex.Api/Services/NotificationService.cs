using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Notifications;
using AdmissionPlex.Core.Entities.Settings;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Services;

namespace AdmissionPlex.Api.Services;

public partial class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IAppSettingService _settings;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext context,
        IAppSettingService settings,
        UserManager<AppUser> userManager,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _settings = settings;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  TEMPLATE-BASED SEND (primary method)
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> SendByTemplateAsync(string templateCode, long userId, Dictionary<string, string> data)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("SendByTemplate: User {UserId} not found", userId);
            return false;
        }

        // Add standard placeholders
        data.TryAdd("Email", user.Email ?? "");
        data.TryAdd("Phone", user.PhoneNumber ?? "");

        var templates = await _context.NotificationTemplates
            .Where(t => t.Code == templateCode && t.IsActive)
            .ToListAsync();

        if (!templates.Any())
        {
            _logger.LogWarning("No active templates found for code '{Code}'", templateCode);
            return false;
        }

        var anySuccess = false;

        foreach (var template in templates)
        {
            var success = template.Channel switch
            {
                "email" when !string.IsNullOrEmpty(user.Email) =>
                    await SendEmailAsync(user.Email, ReplacePlaceholders(template.Subject ?? "", data),
                        ReplacePlaceholders(template.BodyHtml ?? template.BodyText ?? "", data)),

                "sms" when !string.IsNullOrEmpty(user.PhoneNumber) =>
                    await SendSmsAsync(user.PhoneNumber, ReplacePlaceholders(template.BodyText ?? "", data)),

                "whatsapp" when !string.IsNullOrEmpty(user.PhoneNumber) =>
                    await SendWhatsAppAsync(user.PhoneNumber, ReplacePlaceholders(template.BodyText ?? "", data),
                        template.WhatsAppTemplateName, data),

                "push" when !string.IsNullOrEmpty(user.FcmDeviceToken) =>
                    await SendPushAsync(user.FcmDeviceToken,
                        ReplacePlaceholders(template.PushTitle ?? template.Subject ?? "", data),
                        ReplacePlaceholders(template.BodyText ?? "", data),
                        template.ActionUrl, template.PushImageUrl),

                _ => false
            };

            if (success) anySuccess = true;
        }

        // Also create an in-app notification
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = MapTemplateToNotificationType(templateCode),
            Title = ReplacePlaceholders(templates.First().Subject ?? templates.First().PushTitle ?? templateCode, data),
            Message = ReplacePlaceholders(templates.First().BodyText ?? "", data),
            ActionUrl = templates.FirstOrDefault(t => t.ActionUrl != null)?.ActionUrl
        });
        await _context.SaveChangesAsync();

        return anySuccess;
    }

    // ═══════════════════════════════════════════════════════════
    //  EMAIL (SMTP)
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? fromName = null)
    {
        NotificationLog? log = null;
        try
        {
            if (!await _settings.IsCategoryEnabledAsync("smtp"))
            {
                _logger.LogDebug("SMTP is disabled, skipping email to {To}", to);
                return false;
            }

            var cfg = await _settings.GetCategoryAsync("smtp");
            var host = cfg.GetValueOrDefault("Host", "");
            var port = int.TryParse(cfg.GetValueOrDefault("Port", "587"), out var p) ? p : 587;
            var username = cfg.GetValueOrDefault("Username", "");
            var password = cfg.GetValueOrDefault("Password", "");
            var fromEmail = cfg.GetValueOrDefault("FromEmail", username);
            var displayName = fromName ?? cfg.GetValueOrDefault("FromName", "AdmissionPlex");
            var enableSsl = cfg.GetValueOrDefault("EnableSsl", "true") == "true";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("SMTP not configured (missing Host or Username)");
                return false;
            }

            log = CreateLog("email", to, subject, htmlBody);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = 30000
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, displayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);

            log.Status = "Sent";
            log.SentAt = DateTime.UtcNow;
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            if (log != null) { log.Status = "Failed"; log.ErrorMessage = ex.Message; }
            return false;
        }
        finally
        {
            if (log != null) { _context.NotificationLogs.Add(log); await _context.SaveChangesAsync(); }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  SMS (MSG91 / Twilio-style HTTP API)
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        NotificationLog? log = null;
        try
        {
            if (!await _settings.IsCategoryEnabledAsync("sms"))
                return false;

            var cfg = await _settings.GetCategoryAsync("sms");
            var provider = cfg.GetValueOrDefault("Provider", "msg91"); // msg91, twilio, textlocal
            var apiKey = cfg.GetValueOrDefault("ApiKey", "");
            var senderId = cfg.GetValueOrDefault("SenderId", "ADMPLX");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("SMS not configured (missing ApiKey)");
                return false;
            }

            log = CreateLog("sms", phoneNumber, null, message);
            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response;

            switch (provider.ToLower())
            {
                case "msg91":
                    var templateId = cfg.GetValueOrDefault("TemplateId", "");
                    var msg91Body = new
                    {
                        template_id = templateId,
                        short_url = "0",
                        recipients = new[] { new { mobiles = NormalizePhone(phoneNumber), var1 = message } }
                    };
                    var msg91Request = new HttpRequestMessage(HttpMethod.Post, "https://control.msg91.com/api/v5/flow/")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(msg91Body), Encoding.UTF8, "application/json")
                    };
                    msg91Request.Headers.Add("authkey", apiKey);
                    response = await client.SendAsync(msg91Request);
                    break;

                case "twilio":
                    var accountSid = cfg.GetValueOrDefault("AccountSid", "");
                    var fromNumber = cfg.GetValueOrDefault("FromNumber", "");
                    var twilioUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";
                    var twilioContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("To", NormalizePhone(phoneNumber)),
                        new KeyValuePair<string, string>("From", fromNumber),
                        new KeyValuePair<string, string>("Body", message)
                    });
                    var twilioRequest = new HttpRequestMessage(HttpMethod.Post, twilioUrl) { Content = twilioContent };
                    twilioRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{apiKey}")));
                    response = await client.SendAsync(twilioRequest);
                    break;

                case "textlocal":
                    var tlUrl = $"https://api.textlocal.in/send/?apikey={apiKey}&numbers={NormalizePhone(phoneNumber)}&message={Uri.EscapeDataString(message)}&sender={senderId}";
                    response = await client.GetAsync(tlUrl);
                    break;

                default:
                    _logger.LogWarning("Unknown SMS provider: {Provider}", provider);
                    return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            log.ProviderResponse = responseBody;

            if (response.IsSuccessStatusCode)
            {
                log.Status = "Sent"; log.SentAt = DateTime.UtcNow;
                _logger.LogInformation("SMS sent to {Phone} via {Provider}", phoneNumber, provider);
                return true;
            }

            log.Status = "Failed";
            log.ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody[..Math.Min(500, responseBody.Length)]}";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {Phone}", phoneNumber);
            if (log != null) { log.Status = "Failed"; log.ErrorMessage = ex.Message; }
            return false;
        }
        finally
        {
            if (log != null) { _context.NotificationLogs.Add(log); await _context.SaveChangesAsync(); }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  WHATSAPP (Interakt / Wati / Meta Cloud API)
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> SendWhatsAppAsync(string phoneNumber, string message,
        string? templateName = null, Dictionary<string, string>? templateParams = null)
    {
        NotificationLog? log = null;
        try
        {
            if (!await _settings.IsCategoryEnabledAsync("whatsapp"))
                return false;

            var cfg = await _settings.GetCategoryAsync("whatsapp");
            var provider = cfg.GetValueOrDefault("Provider", "meta"); // meta, interakt, wati
            var apiKey = cfg.GetValueOrDefault("ApiKey", "");
            var phoneNumberId = cfg.GetValueOrDefault("PhoneNumberId", "");

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("WhatsApp not configured (missing ApiKey)");
                return false;
            }

            log = CreateLog("whatsapp", phoneNumber, null, message);
            var client = _httpClientFactory.CreateClient();
            HttpResponseMessage response;

            switch (provider.ToLower())
            {
                case "meta":
                    // Meta Cloud API (official)
                    var metaUrl = $"https://graph.facebook.com/v18.0/{phoneNumberId}/messages";
                    object metaBody;
                    if (!string.IsNullOrEmpty(templateName))
                    {
                        metaBody = new
                        {
                            messaging_product = "whatsapp",
                            to = NormalizePhone(phoneNumber),
                            type = "template",
                            template = new
                            {
                                name = templateName,
                                language = new { code = "en" },
                                components = templateParams != null ? new[] {
                                    new { type = "body", parameters = templateParams.Values
                                        .Select(v => new { type = "text", text = v }).ToArray() }
                                } : null
                            }
                        };
                    }
                    else
                    {
                        metaBody = new
                        {
                            messaging_product = "whatsapp",
                            to = NormalizePhone(phoneNumber),
                            type = "text",
                            text = new { body = message }
                        };
                    }

                    var metaRequest = new HttpRequestMessage(HttpMethod.Post, metaUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(metaBody), Encoding.UTF8, "application/json")
                    };
                    metaRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    response = await client.SendAsync(metaRequest);
                    break;

                case "interakt":
                    var interaktUrl = "https://api.interakt.ai/v1/public/message/";
                    var interaktBody = new
                    {
                        countryCode = "+91",
                        phoneNumber = NormalizePhone(phoneNumber).TrimStart('+'),
                        callbackData = "notification",
                        type = "Text",
                        data = new { message }
                    };
                    var interaktRequest = new HttpRequestMessage(HttpMethod.Post, interaktUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(interaktBody), Encoding.UTF8, "application/json")
                    };
                    interaktRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", apiKey);
                    response = await client.SendAsync(interaktRequest);
                    break;

                case "wati":
                    var watiBaseUrl = cfg.GetValueOrDefault("BaseUrl", "https://live-server.wati.io");
                    var watiUrl = $"{watiBaseUrl}/api/v1/sendSessionMessage/{NormalizePhone(phoneNumber)}";
                    var watiRequest = new HttpRequestMessage(HttpMethod.Post, watiUrl)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(new { messageText = message }),
                            Encoding.UTF8, "application/json")
                    };
                    watiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    response = await client.SendAsync(watiRequest);
                    break;

                default:
                    _logger.LogWarning("Unknown WhatsApp provider: {Provider}", provider);
                    return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            log.ProviderResponse = responseBody;

            if (response.IsSuccessStatusCode)
            {
                log.Status = "Sent"; log.SentAt = DateTime.UtcNow;
                _logger.LogInformation("WhatsApp sent to {Phone} via {Provider}", phoneNumber, provider);
                return true;
            }

            log.Status = "Failed";
            log.ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody[..Math.Min(500, responseBody.Length)]}";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", phoneNumber);
            if (log != null) { log.Status = "Failed"; log.ErrorMessage = ex.Message; }
            return false;
        }
        finally
        {
            if (log != null) { _context.NotificationLogs.Add(log); await _context.SaveChangesAsync(); }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PUSH (Firebase Cloud Messaging - HTTP v1 API)
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> SendPushAsync(string deviceToken, string title, string body,
        string? actionUrl = null, string? imageUrl = null)
    {
        NotificationLog? log = null;
        try
        {
            if (!await _settings.IsCategoryEnabledAsync("push"))
                return false;

            var cfg = await _settings.GetCategoryAsync("push");
            var serverKey = cfg.GetValueOrDefault("ServerKey", "");
            var projectId = cfg.GetValueOrDefault("ProjectId", "");

            if (string.IsNullOrEmpty(serverKey))
            {
                _logger.LogWarning("Push not configured (missing ServerKey)");
                return false;
            }

            log = CreateLog("push", deviceToken[..Math.Min(30, deviceToken.Length)] + "...", title, body);

            var client = _httpClientFactory.CreateClient();

            // FCM legacy HTTP API (simpler, uses server key directly)
            var fcmUrl = "https://fcm.googleapis.com/fcm/send";
            var fcmBody = new
            {
                to = deviceToken,
                notification = new
                {
                    title,
                    body,
                    image = imageUrl,
                    click_action = actionUrl
                },
                data = new
                {
                    action_url = actionUrl
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, fcmUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(fcmBody), Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("Authorization", $"key={serverKey}");

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            log.ProviderResponse = responseBody;

            if (response.IsSuccessStatusCode)
            {
                log.Status = "Sent"; log.SentAt = DateTime.UtcNow;
                _logger.LogInformation("Push sent to token {Token}", deviceToken[..Math.Min(20, deviceToken.Length)]);
                return true;
            }

            log.Status = "Failed";
            log.ErrorMessage = $"HTTP {(int)response.StatusCode}";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification");
            if (log != null) { log.Status = "Failed"; log.ErrorMessage = ex.Message; }
            return false;
        }
        finally
        {
            if (log != null) { _context.NotificationLogs.Add(log); await _context.SaveChangesAsync(); }
        }
    }

    public async Task<bool> SendPushToUserAsync(long userId, string title, string body, string? actionUrl = null)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.FcmDeviceToken == null) return false;
        return await SendPushAsync(user.FcmDeviceToken, title, body, actionUrl);
    }

    // ═══════════════════════════════════════════════════════════
    //  TEST (Admin sends a test notification to verify config)
    // ═══════════════════════════════════════════════════════════

    public async Task<(bool Success, string Message)> SendTestAsync(string channel, string recipient)
    {
        try
        {
            var success = channel switch
            {
                "email" => await SendEmailAsync(recipient, "AdmissionPlex Test Email",
                    "<h2>Test Email</h2><p>Your SMTP configuration is working correctly.</p><p>— AdmissionPlex</p>"),
                "sms" => await SendSmsAsync(recipient, "AdmissionPlex: Your SMS configuration is working correctly."),
                "whatsapp" => await SendWhatsAppAsync(recipient, "AdmissionPlex: Your WhatsApp configuration is working correctly."),
                "push" => await SendPushAsync(recipient, "AdmissionPlex Test", "Your push notification configuration is working correctly."),
                _ => false
            };

            return success
                ? (true, $"Test {channel} sent successfully to {recipient}")
                : (false, $"Failed to send test {channel}. Check the logs for details.");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private static string ReplacePlaceholders(string template, Dictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return data.GetValueOrDefault(key, match.Value);
        });
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex PlaceholderRegex();

    private static string NormalizePhone(string phone)
    {
        // Ensure Indian numbers start with +91
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 10) return "+91" + digits;
        if (digits.StartsWith("91") && digits.Length == 12) return "+" + digits;
        return phone.StartsWith('+') ? phone : "+" + digits;
    }

    private NotificationLog CreateLog(string channel, string recipient, string? subject, string? body)
    {
        return new NotificationLog
        {
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body?[..Math.Min(2000, body.Length)],
            Status = "Queued"
        };
    }

    private static NotificationType MapTemplateToNotificationType(string code) => code switch
    {
        "welcome" or "registration" => NotificationType.System,
        "test_complete" or "test_reminder" => NotificationType.TestReminder,
        "result_ready" or "report_ready" => NotificationType.ResultReady,
        "payment_success" or "payment_failed" => NotificationType.Payment,
        "referral_converted" => NotificationType.ReferralConverted,
        "session_reminder" => NotificationType.SessionReminder,
        _ => NotificationType.System
    };
}
