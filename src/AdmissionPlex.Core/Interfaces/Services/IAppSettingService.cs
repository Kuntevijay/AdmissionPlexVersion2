namespace AdmissionPlex.Core.Interfaces.Services;

public interface IAppSettingService
{
    /// <summary>Get a single setting value by category + key.</summary>
    Task<string?> GetAsync(string category, string key);

    /// <summary>Get all settings for a category as a dictionary.</summary>
    Task<Dictionary<string, string>> GetCategoryAsync(string category);

    /// <summary>Check if a category is enabled (any key with IsEnabled=true).</summary>
    Task<bool> IsCategoryEnabledAsync(string category);

    /// <summary>Save (upsert) a single setting.</summary>
    Task SetAsync(string category, string key, string value, bool isSensitive = false, string? description = null, long? updatedBy = null);

    /// <summary>Save multiple settings for a category at once.</summary>
    Task SetCategoryAsync(string category, Dictionary<string, string> settings, bool enabled = true, long? updatedBy = null);

    /// <summary>Enable/disable an entire category.</summary>
    Task SetCategoryEnabledAsync(string category, bool enabled, long? updatedBy = null);

    /// <summary>Get all settings grouped by category (for admin settings page).</summary>
    Task<Dictionary<string, Dictionary<string, string>>> GetAllGroupedAsync();

    /// <summary>Clear cached settings (call after save).</summary>
    void InvalidateCache(string? category = null);
}
