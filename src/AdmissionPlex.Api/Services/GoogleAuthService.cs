using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Users;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Shared.Constants;
using AdmissionPlex.Shared.DTOs.Auth;

namespace AdmissionPlex.Api.Services;

/// <summary>
/// Handles Google sign-in by verifying the ID token with Google's tokeninfo endpoint,
/// then either logging in an existing user or creating a new student account.
/// Google OAuth ClientId is read from AppSettings (category: google_auth).
/// </summary>
public class GoogleAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAppSettingService _appSettings;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        UserManager<AppUser> userManager,
        IJwtTokenService jwtTokenService,
        IAppSettingService appSettings,
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleAuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _appSettings = appSettings;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(bool Enabled, string? ClientId)> GetGoogleConfigAsync()
    {
        var enabled = await _appSettings.IsCategoryEnabledAsync("google_auth");
        if (!enabled) return (false, null);
        var clientId = await _appSettings.GetAsync("google_auth", "ClientId");
        return (enabled && !string.IsNullOrEmpty(clientId), clientId);
    }

    public async Task<(bool Success, AuthResponse? Data, string? Error)> HandleGoogleLoginAsync(string idToken)
    {
        // 1. Verify the ID token with Google
        var googleUser = await VerifyGoogleTokenAsync(idToken);
        if (googleUser == null)
            return (false, null, "Invalid or expired Google token.");

        // 2. Validate that the token was issued for our client ID
        var expectedClientId = await _appSettings.GetAsync("google_auth", "ClientId");
        if (string.IsNullOrEmpty(expectedClientId))
            return (false, null, "Google Sign-In is not configured.");

        if (googleUser.Aud != expectedClientId)
        {
            _logger.LogWarning("Google token audience mismatch: expected {Expected}, got {Got}",
                expectedClientId, googleUser.Aud);
            return (false, null, "Token was not issued for this application.");
        }

        if (!googleUser.EmailVerified)
            return (false, null, "Google email is not verified.");

        // 3. Look for existing user
        var user = await _userManager.FindByEmailAsync(googleUser.Email);

        if (user != null)
        {
            // Existing user — update Google info and login
            if (!user.IsActive)
                return (false, null, "Your account has been deactivated. Please contact support.");

            user.LoginProvider ??= "Google";
            user.ProviderKey ??= googleUser.Sub;
            user.ProfilePictureUrl = googleUser.Picture;
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);
            var displayName = await GetDisplayNameAsync(user, roles);

            _logger.LogInformation("Google login for existing user: {Email}", googleUser.Email);

            return (true, new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? AppRoles.Student,
                FullName = displayName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            }, null);
        }

        // 4. New user — auto-register as Student
        var autoRegister = await _appSettings.GetAsync("google_auth", "AutoRegister");
        if (autoRegister?.ToLower() == "false")
            return (false, null, "No account found with this email. Please register first.");

        user = new AppUser
        {
            UserName = googleUser.Email,
            Email = googleUser.Email,
            EmailConfirmed = true,
            LoginProvider = "Google",
            ProviderKey = googleUser.Sub,
            ProfilePictureUrl = googleUser.Picture,
            LastLoginAt = DateTime.UtcNow
        };

        // Google login users get a random secure password (they'll never use it)
        var randomPassword = Guid.NewGuid().ToString("N") + "!Aa1";
        var createResult = await _userManager.CreateAsync(user, randomPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create Google user {Email}: {Errors}", googleUser.Email, errors);
            return (false, null, $"Account creation failed: {errors}");
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Student);

        // Create student profile from Google data
        var names = (googleUser.Name ?? googleUser.Email.Split('@')[0]).Split(' ', 2);
        _context.StudentProfiles.Add(new StudentProfile
        {
            UserId = user.Id,
            FirstName = googleUser.GivenName ?? names[0],
            LastName = googleUser.FamilyName ?? (names.Length > 1 ? names[1] : "")
        });
        await _context.SaveChangesAsync();

        var newRoles = await _userManager.GetRolesAsync(user);
        var newToken = _jwtTokenService.GenerateToken(user, newRoles);

        _logger.LogInformation("New Google user registered: {Email}", googleUser.Email);

        return (true, new AuthResponse
        {
            Token = newToken,
            Email = user.Email!,
            Role = AppRoles.Student,
            FullName = $"{googleUser.GivenName ?? names[0]} {googleUser.FamilyName ?? ""}".Trim(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        }, null);
    }

    // ──────────── Token Verification ────────────

    private async Task<GoogleUserInfo?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token verification failed: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GoogleUserInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google token");
            return null;
        }
    }

    private async Task<string> GetDisplayNameAsync(AppUser user, IList<string> roles)
    {
        var role = roles.FirstOrDefault();
        return role switch
        {
            AppRoles.Student => (await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == user.Id))
                is { } s ? $"{s.FirstName} {s.LastName}" : user.Email!,
            AppRoles.Counsellor => (await _context.CounsellorProfiles.FirstOrDefaultAsync(c => c.UserId == user.Id))
                ?.FullName ?? user.Email!,
            AppRoles.Coordinator => (await _context.CoordinatorProfiles.FirstOrDefaultAsync(c => c.UserId == user.Id))
                ?.FullName ?? user.Email!,
            _ => user.Email!
        };
    }
}

// ──────────── Google Token Response Model ────────────

public class GoogleUserInfo
{
    public string Sub { get; set; } = "";           // Google unique user ID
    public string Email { get; set; } = "";
    public bool EmailVerified { get; set; }
    public string? Name { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
    public string Aud { get; set; } = "";           // Client ID this token was issued for
    public string Iss { get; set; } = "";
    public long Exp { get; set; }

    // Snake_case alternatives (Google returns both)
    public bool Email_verified { set => EmailVerified = value; }
    public string? Given_name { set => GivenName = value; }
    public string? Family_name { set => FamilyName = value; }
}
