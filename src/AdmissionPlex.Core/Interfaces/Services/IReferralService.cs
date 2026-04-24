namespace AdmissionPlex.Core.Interfaces.Services;

public interface IReferralService
{
    Task<string> GenerateCodeAsync(long userId);
    Task<(bool Success, string? Error)> ApplyCodeAsync(long userId, string code);
    Task<object> GetStatsAsync(long userId);
}
