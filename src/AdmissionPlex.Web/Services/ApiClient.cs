using System.Net.Http.Headers;

namespace AdmissionPlex.Web.Services;

/// <summary>
/// HttpClient wrapper that automatically attaches JWT token to requests.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly TokenStorageService _tokenStorage;

    public ApiClient(HttpClient http, TokenStorageService tokenStorage)
    {
        _http = http;
        _tokenStorage = tokenStorage;
    }

    private async Task AttachTokenAsync()
    {
        var token = await _tokenStorage.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        await AttachTokenAsync();
        return await _http.GetAsync(url);
    }

    public async Task<T?> GetFromJsonAsync<T>(string url)
    {
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<T>(url);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
    {
        await AttachTokenAsync();
        return await _http.PostAsJsonAsync(url, data);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T data)
    {
        await AttachTokenAsync();
        return await _http.PutAsJsonAsync(url, data);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        await AttachTokenAsync();
        return await _http.DeleteAsync(url);
    }

    public async Task<HttpResponseMessage> PostMultipartAsync(string url, MultipartFormDataContent content)
    {
        await AttachTokenAsync();
        return await _http.PostAsync(url, content);
    }

    /// <summary>Resolves a relative path (e.g. "/uploads/x.jpg") against the API base URL for use in &lt;img src="..."&gt;.</summary>
    public string ResolveUrl(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        if (path.StartsWith("http://") || path.StartsWith("https://")) return path;
        var baseAddr = _http.BaseAddress?.ToString().TrimEnd('/') ?? "";
        return path.StartsWith("/") ? $"{baseAddr}{path}" : $"{baseAddr}/{path}";
    }
}
