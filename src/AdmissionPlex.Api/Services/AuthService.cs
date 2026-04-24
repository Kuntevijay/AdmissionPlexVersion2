using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Core.Entities.Users;
using AdmissionPlex.Shared.Constants;
using AdmissionPlex.Shared.DTOs.Auth;

namespace AdmissionPlex.Api.Services;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly AppDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtTokenService jwtTokenService,
        AppDbContext context,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, AuthResponse? Data, string? Error)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return (false, null, "Invalid email or password.");

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                return (false, null, "Account is locked. Please try again later.");
            return (false, null, "Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.GenerateToken(user, roles);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Get display name from profile
        var displayName = await GetDisplayNameAsync(user, roles);

        return (true, new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            Role = roles.FirstOrDefault() ?? "Student",
            FullName = displayName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        }, null);
    }

    public async Task<(bool Success, AuthResponse? Data, List<string>? Errors)> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return (false, null, new List<string> { "An account with this email already exists." });

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.Phone,
            EmailConfirmed = true // Set to false if you want email verification
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return (false, null, result.Errors.Select(e => e.Description).ToList());

        // Assign Student role by default
        await _userManager.AddToRoleAsync(user, AppRoles.Student);

        // Create student profile
        var profile = new StudentProfile
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ReferredByCode = request.ReferralCode
        };
        _context.StudentProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Generate token
        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.GenerateToken(user, roles);

        _logger.LogInformation("New student registered: {Email}", request.Email);

        return (true, new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            Role = AppRoles.Student,
            FullName = $"{request.FirstName} {request.LastName}",
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        }, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "User not found.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return (false, result.Errors.First().Description);

        return (true, null);
    }

    public async Task<(bool Success, AuthResponse? Data, string? Error)> CreateUserAsync(
        string email, string password, string role, string fullName, string? phone = null)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = phone,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            return (false, null, string.Join("; ", result.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, role);

        // Create the appropriate profile
        switch (role)
        {
            case AppRoles.Student:
                var names = fullName.Split(' ', 2);
                _context.StudentProfiles.Add(new StudentProfile
                {
                    UserId = user.Id,
                    FirstName = names[0],
                    LastName = names.Length > 1 ? names[1] : ""
                });
                break;

            case AppRoles.Counsellor:
                _context.CounsellorProfiles.Add(new CounsellorProfile
                {
                    UserId = user.Id,
                    FullName = fullName,
                    Qualification = "TBD"
                });
                break;

            case AppRoles.Coordinator:
                _context.CoordinatorProfiles.Add(new CoordinatorProfile
                {
                    UserId = user.Id,
                    FullName = fullName
                });
                break;
        }

        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenService.GenerateToken(user, roles);

        return (true, new AuthResponse
        {
            Token = token,
            Email = user.Email!,
            Role = role,
            FullName = fullName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        }, null);
    }

    private async Task<string> GetDisplayNameAsync(AppUser user, IList<string> roles)
    {
        var role = roles.FirstOrDefault();
        switch (role)
        {
            case AppRoles.Student:
                var student = await _context.StudentProfiles
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                return student != null ? $"{student.FirstName} {student.LastName}" : user.Email!;

            case AppRoles.Counsellor:
                var counsellor = await _context.CounsellorProfiles
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
                return counsellor?.FullName ?? user.Email!;

            case AppRoles.Coordinator:
                var coordinator = await _context.CoordinatorProfiles
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);
                return coordinator?.FullName ?? user.Email!;

            default:
                return user.Email!;
        }
    }
}
