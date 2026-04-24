using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Api.Services;
using AdmissionPlex.Shared.Common;
using AdmissionPlex.Shared.DTOs.Auth;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
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
