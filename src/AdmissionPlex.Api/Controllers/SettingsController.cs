using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Settings;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly IAppSettingService _settings;
    private readonly INotificationService _notifications;
    private readonly AppDbContext _context;

    public SettingsController(IAppSettingService settings, INotificationService notifications, AppDbContext context)
    {
        _settings = settings;
        _notifications = notifications;
        _context = context;
    }

    // ═══════════════════════════════════════════════════════════
    //  APP SETTINGS (generic key-value by category)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Get all settings grouped by category (masked sensitive values).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var grouped = await _settings.GetAllGroupedAsync();

        // Also return enabled status per category
        var categories = await _context.AppSettings
            .GroupBy(s => s.Category)
            .Select(g => new { Category = g.Key, IsEnabled = g.Any(s => s.IsEnabled) })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { Settings = grouped, Categories = categories }));
    }

    /// <summary>Get settings for a specific category.</summary>
    [HttpGet("{category}")]
    public async Task<IActionResult> GetCategory(string category)
    {
        var settings = await _context.AppSettings
            .Where(s => s.Category == category)
            .Select(s => new
            {
                s.Key,
                Value = s.IsSensitive ? "••••••••" : s.Value,
                s.IsSensitive,
                s.IsEnabled,
                s.Description,
                s.UpdatedAt
            })
            .ToListAsync();

        var enabled = settings.Any(s => s.IsEnabled);
        return Ok(ApiResponse<object>.Ok(new { Category = category, IsEnabled = enabled, Settings = settings }));
    }

    /// <summary>Save all settings for a category at once.</summary>
    [HttpPut("{category}")]
    public async Task<IActionResult> SaveCategory(string category, [FromBody] SaveCategoryDto dto)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // For sensitive fields, only update if the value is not the mask
        var existingSettings = await _context.AppSettings
            .Where(s => s.Category == category)
            .ToDictionaryAsync(s => s.Key, s => s);

        var toSave = new Dictionary<string, string>();
        foreach (var (key, value) in dto.Settings)
        {
            if (value == "••••••••" && existingSettings.TryGetValue(key, out var existing))
                toSave[key] = existing.Value; // Keep existing value
            else
                toSave[key] = value;
        }

        await _settings.SetCategoryAsync(category, toSave, dto.IsEnabled, userId);
        return Ok(ApiResponse<object>.Ok(new { }, $"Settings for '{category}' saved successfully."));
    }

    /// <summary>Toggle enable/disable for a category.</summary>
    [HttpPatch("{category}/toggle")]
    public async Task<IActionResult> ToggleCategory(string category, [FromBody] ToggleDto dto)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _settings.SetCategoryEnabledAsync(category, dto.IsEnabled, userId);
        return Ok(ApiResponse<object>.Ok(new { }, $"{category} {(dto.IsEnabled ? "enabled" : "disabled")}."));
    }

    // ═══════════════════════════════════════════════════════════
    //  NOTIFICATION TEMPLATES
    // ═══════════════════════════════════════════════════════════

    /// <summary>List all notification templates.</summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _context.NotificationTemplates
            .OrderBy(t => t.Code).ThenBy(t => t.Channel)
            .Select(t => new
            {
                t.Id, t.Code, t.Name, t.Channel, t.Subject,
                BodyPreview = t.BodyHtml != null ? t.BodyHtml.Substring(0, Math.Min(100, t.BodyHtml.Length)) : t.BodyText,
                t.WhatsAppTemplateName, t.PushTitle, t.IsActive
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(templates));
    }

    /// <summary>Get a single template by ID.</summary>
    [HttpGet("templates/{id}")]
    public async Task<IActionResult> GetTemplate(long id)
    {
        var t = await _context.NotificationTemplates.FindAsync(id);
        if (t == null) return NotFound(ApiResponse<object>.Fail("Template not found."));
        return Ok(ApiResponse<NotificationTemplate>.Ok(t));
    }

    /// <summary>Create or update a notification template.</summary>
    [HttpPost("templates")]
    public async Task<IActionResult> SaveTemplate([FromBody] NotificationTemplate dto)
    {
        if (dto.Id > 0)
        {
            var existing = await _context.NotificationTemplates.FindAsync(dto.Id);
            if (existing == null) return NotFound(ApiResponse<object>.Fail("Template not found."));

            existing.Code = dto.Code;
            existing.Name = dto.Name;
            existing.Channel = dto.Channel;
            existing.Subject = dto.Subject;
            existing.BodyHtml = dto.BodyHtml;
            existing.BodyText = dto.BodyText;
            existing.WhatsAppTemplateName = dto.WhatsAppTemplateName;
            existing.PushTitle = dto.PushTitle;
            existing.PushImageUrl = dto.PushImageUrl;
            existing.ActionUrl = dto.ActionUrl;
            existing.IsActive = dto.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.NotificationTemplates.Add(dto);
        }

        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { dto.Id }, "Template saved."));
    }

    /// <summary>Delete a template.</summary>
    [HttpDelete("templates/{id}")]
    public async Task<IActionResult> DeleteTemplate(long id)
    {
        var t = await _context.NotificationTemplates.FindAsync(id);
        if (t == null) return NotFound();
        _context.NotificationTemplates.Remove(t);
        await _context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Template deleted."));
    }

    // ═══════════════════════════════════════════════════════════
    //  NOTIFICATION LOGS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Get notification logs with filtering and pagination.</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? channel = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.NotificationLogs.AsQueryable();
        if (!string.IsNullOrEmpty(channel)) query = query.Where(l => l.Channel == channel);
        if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id, l.Channel, l.Recipient, l.Subject,
                BodyPreview = l.Body != null ? l.Body.Substring(0, Math.Min(80, l.Body.Length)) : null,
                l.Status, l.ErrorMessage, l.CreatedAt, l.SentAt, l.TemplateCode
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(new { Total = total, Page = page, PageSize = pageSize, Logs = logs }));
    }

    /// <summary>Get log details by ID.</summary>
    [HttpGet("logs/{id}")]
    public async Task<IActionResult> GetLogDetail(long id)
    {
        var log = await _context.NotificationLogs.FindAsync(id);
        if (log == null) return NotFound();
        return Ok(ApiResponse<NotificationLog>.Ok(log));
    }

    // ═══════════════════════════════════════════════════════════
    //  TEST NOTIFICATIONS
    // ═══════════════════════════════════════════════════════════

    /// <summary>Send a test notification to verify provider config.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTest([FromBody] SendTestDto dto)
    {
        var (success, message) = await _notifications.SendTestAsync(dto.Channel, dto.Recipient);
        return success
            ? Ok(ApiResponse<object>.Ok(new { }, message))
            : BadRequest(ApiResponse<object>.Fail(message));
    }
}

// ─── DTOs ───
public class SaveCategoryDto
{
    public Dictionary<string, string> Settings { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
}

public class ToggleDto
{
    public bool IsEnabled { get; set; }
}

public class SendTestDto
{
    public string Channel { get; set; } = "";
    public string Recipient { get; set; } = "";
}
