namespace AdmissionPlex.Core.Interfaces.Services;

public interface IAuthService
{
    Task<(bool Success, string Token, string? Error)> LoginAsync(string email, string password);
    Task<(bool Success, string? Error)> RegisterAsync(string email, string password, string role, string firstName, string lastName);
    Task<(bool Success, string Token, string? Error)> RefreshTokenAsync(string token);
    Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword);
}
