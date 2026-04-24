using Microsoft.JSInterop;

namespace AdmissionPlex.Web.Services;

/// <summary>
/// Stores and retrieves JWT token from browser localStorage via JS interop.
/// </summary>
public class TokenStorageService
{
    private readonly IJSRuntime _js;
    private string? _cachedToken;

    public TokenStorageService(IJSRuntime js) => _js = js;

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken != null) return _cachedToken;

        try
        {
            _cachedToken = await _js.InvokeAsync<string?>("localStorage.getItem", "auth_token");
        }
        catch
        {
            // SSR prerender - no JS available yet
            _cachedToken = null;
        }
        return _cachedToken;
    }

    public async Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
        }
        catch { }
    }

    public async Task RemoveTokenAsync()
    {
        _cachedToken = null;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        }
        catch { }
    }

    public async Task<string?> GetUserRoleAsync()
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", "auth_role");
        }
        catch { return null; }
    }

    public async Task SetUserInfoAsync(string role, string fullName, string email)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "auth_role", role);
            await _js.InvokeVoidAsync("localStorage.setItem", "auth_name", fullName);
            await _js.InvokeVoidAsync("localStorage.setItem", "auth_email", email);
        }
        catch { }
    }

    public async Task ClearAllAsync()
    {
        _cachedToken = null;
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_token");
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_role");
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_name");
            await _js.InvokeVoidAsync("localStorage.removeItem", "auth_email");
        }
        catch { }
    }
}
