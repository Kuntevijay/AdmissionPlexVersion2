using Microsoft.AspNetCore.Identity;
using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Api.Data;

/// <summary>
/// ASP.NET Identity user with long primary key.
/// Links to our domain User entity for profile data.
/// </summary>
public class AppUser : IdentityUser<long>
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Social login
    public string? LoginProvider { get; set; }        // "Local", "Google"
    public string? ProviderKey { get; set; }          // Google sub (subject ID)
    public string? ProfilePictureUrl { get; set; }

    // Push notifications
    public string? FcmDeviceToken { get; set; }       // Firebase Cloud Messaging token
    public DateTime? FcmTokenUpdatedAt { get; set; }

    // Navigation to domain profiles
    public StudentProfile? StudentProfile { get; set; }
    public CounsellorProfile? CounsellorProfile { get; set; }
    public CoordinatorProfile? CoordinatorProfile { get; set; }
}

/// <summary>
/// Identity role with long primary key.
/// </summary>
public class AppRole : IdentityRole<long>
{
    public AppRole() { }
    public AppRole(string roleName) : base(roleName) { }
}
