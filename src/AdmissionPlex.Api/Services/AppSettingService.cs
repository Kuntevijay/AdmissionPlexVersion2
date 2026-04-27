using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Settings;
using AdmissionPlex.Core.Interfaces.Services;

namespace AdmissionPlex.Api.Services;

public class AppSettingService : IAppSettingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AppSettingService> _logger;

    // Simple in-memory cache: category → (key → value). Cleared on save.
    private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private const int CacheMinutes = 5;

    public AppSettingService(AppDbContext context, ILogger<AppSettingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string category, string key)
    {
        var dict = await GetCategoryAsync(category);
        return dict.GetValueOrDefault(key);
    }

    public async Task<Dictionary<string, string>> GetCategoryAsync(string category)
    {
        if (DateTime.UtcNow > _cacheExpiry)
            InvalidateCache();

        if (_cache.TryGetValue(category, out var cached))
            return cached;

        var settings = await _context.AppSettings
            .Where(s => s.Category == category)
            .ToListAsync();

        var dict = settings.ToDictionary(s => s.Key, s => s.Value);
        _cache[category] = dict;
        _cacheExpiry = DateTime.UtcNow.AddMinutes(CacheMinutes);

        return dict;
    }

    public async Task<bool> IsCategoryEnabledAsync(string category)
    {
        return await _context.AppSettings
            .AnyAsync(s => s.Category == category && s.IsEnabled);
    }

    public async Task SetAsync(string category, string key, string value,
        bool isSensitive = false, string? description = null, long? updatedBy = null)
    {
        var existing = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

        if (existing != null)
        {
            existing.Value = value;
            existing.IsSensitive = isSensitive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = updatedBy;
            if (description != null) existing.Description = description;
        }
        else
        {
            _context.AppSettings.Add(new AppSetting
            {
                Category = category,
                Key = key,
                Value = value,
                IsSensitive = isSensitive,
                IsEnabled = true,
                Description = description,
                UpdatedBy = updatedBy
            });
        }

        await _context.SaveChangesAsync();
        InvalidateCache(category);
    }

    public async Task SetCategoryAsync(string category, Dictionary<string, string> settings,
        bool enabled = true, long? updatedBy = null)
    {
        var existing = await _context.AppSettings
            .Where(s => s.Category == category)
            .ToListAsync();

        foreach (var (key, value) in settings)
        {
            var setting = existing.FirstOrDefault(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                setting.IsEnabled = enabled;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = updatedBy;
            }
            else
            {
                _context.AppSettings.Add(new AppSetting
                {
                    Category = category,
                    Key = key,
                    Value = value,
                    IsEnabled = enabled,
                    UpdatedBy = updatedBy
                });
            }
        }

        await _context.SaveChangesAsync();
        InvalidateCache(category);
        _logger.LogInformation("Settings updated for category '{Category}' by user {User}",
            category, updatedBy);
    }

    public async Task SetCategoryEnabledAsync(string category, bool enabled, long? updatedBy = null)
    {
        await _context.AppSettings
            .Where(s => s.Category == category)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsEnabled, enabled)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                .SetProperty(x => x.UpdatedBy, updatedBy));

        InvalidateCache(category);
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetAllGroupedAsync()
    {
        var all = await _context.AppSettings.ToListAsync();
        return all
            .GroupBy(s => s.Category)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    s => s.Key,
                    s => s.IsSensitive ? "••••••••" : s.Value   // Mask sensitive values
                )
            );
    }

    public void InvalidateCache(string? category = null)
    {
        if (category != null)
            _cache.TryRemove(category, out _);
        else
            _cache.Clear();
    }
}
