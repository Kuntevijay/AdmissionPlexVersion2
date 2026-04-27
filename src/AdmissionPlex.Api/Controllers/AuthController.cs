using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Api.Services;
using AdmissionPlex.Shared.Common;
using AdmissionPlex.Shared.DTOs.Auth;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly GoogleAuthService _googleAuthService;
    private readonly UserManager<AppUser> _userManager;

    public AuthController(AuthService authService, GoogleAuthService googleAuthService, UserManager<AppUser> userManager)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
        _userManager = userManager;
    }

    /// <summary>
    /// Login with email and password. Returns JWT token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, data, error) = await _authService.LoginAsync(request);
        if (!success)
            return Unauthorized(ApiResponse<object>.Fail(error!));

        return Ok(ApiResponse<AuthResponse>.Ok(data!));
    }

    /// <summary>
    /// Register a new student account.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match."));

        var (success, data, errors) = await _authService.RegisterAsync(request);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail(errors!));

        return Created("", ApiResponse<AuthResponse>.Ok(data!, "Registration successful."));
    }

    /// <summary>
    /// Google sign-in. Verifies the Google ID token, creates/links account, returns JWT.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
            return BadRequest(ApiResponse<object>.Fail("Google ID token is required."));

        var (success, data, error) = await _googleAuthService.HandleGoogleLoginAsync(request.IdToken);
        if (!success)
            return Unauthorized(ApiResponse<object>.Fail(error!));

        return Ok(ApiResponse<AuthResponse>.Ok(data!));
    }

    /// <summary>
    /// Get auth configuration (is Google login enabled, client ID for frontend).
    /// Public endpoint — frontend needs this before showing the Google button.
    /// </summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetAuthConfig()
    {
        var (enabled, clientId) = await _googleAuthService.GetGoogleConfigAsync();
        return Ok(ApiResponse<object>.Ok(new
        {
            GoogleEnabled = enabled,
            GoogleClientId = clientId
        }));
    }

    /// <summary>
    /// Change password for the authenticated user.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        if (!success)
            return BadRequest(ApiResponse<object>.Fail(error!));

        return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully."));
    }

    /// <summary>
    /// Update the FCM device token for push notifications.
    /// </summary>
    [Authorize]
    [HttpPost("device-token")]
    public async Task<IActionResult> UpdateDeviceToken([FromBody] DeviceTokenRequest request)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        user.FcmDeviceToken = request.Token;
        user.FcmTokenUpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse<object>.Ok(new { }, "Device token updated."));
    }

    /// <summary>
    /// Get the current authenticated user's info.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var uuid = User.FindFirst("uuid")?.Value;

        return Ok(ApiResponse<object>.Ok(new
        {
            UserId = userId,
            Email = email,
            Role = role,
            Uuid = uuid
        }));
    }
}

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = "";
}

public class DeviceTokenRequest
{
    public string Token { get; set; } = "";
}
