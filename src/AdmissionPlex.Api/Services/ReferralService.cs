using AdmissionPlex.Core.Entities.Referrals;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Shared.Constants;

namespace AdmissionPlex.Api.Services;

public class ReferralService : IReferralService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(IUnitOfWork uow, ILogger<ReferralService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<string> GenerateCodeAsync(long userId)
    {
        var existing = await _uow.Referrals.GetCodeByUserIdAsync(userId);
        if (existing != null) return existing.Code;

        var code = GenerateUniqueCode();
        var referralCode = new ReferralCode
        {
            UserId = userId,
            Code = code,
            Type = "student",
            IsActive = true
        };

        // Need to save via context - add workaround
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Referral code generated: {Code} for user {UserId}", code, userId);
        return code;
    }

    public async Task<(bool Success, string? Error)> ApplyCodeAsync(long userId, string code)
    {
        var referralCode = await _uow.Referrals.GetCodeByCodeAsync(code);
        if (referralCode == null)
            return (false, "Invalid referral code.");

        if (referralCode.UserId == userId)
            return (false, "You cannot use your own referral code.");

        if (referralCode.MaxUses.HasValue && referralCode.TimesUsed >= referralCode.MaxUses.Value)
            return (false, "This referral code has reached its maximum uses.");

        var existingReferrals = await _uow.Referrals.GetByReferrerAsync(referralCode.UserId);
        if (existingReferrals.Any(r => r.ReferredUserId == userId))
            return (false, "This referral has already been applied.");

        var referral = new Referral
        {
            ReferrerUserId = referralCode.UserId,
            ReferredUserId = userId,
            ReferralCodeId = referralCode.Id,
            Status = ReferralStatus.Pending
        };

        await _uow.Referrals.AddAsync(referral);
        referralCode.TimesUsed++;
        await _uow.SaveChangesAsync();

        return (true, null);
    }

    public async Task<object> GetStatsAsync(long userId)
    {
        var code = await _uow.Referrals.GetCodeByUserIdAsync(userId);
        var referrals = await _uow.Referrals.GetByReferrerAsync(userId);

        return new
        {
            Code = code?.Code ?? "N/A",
            TotalReferrals = referrals.Count(),
            Converted = referrals.Count(r => r.Status == ReferralStatus.Converted),
            Pending = referrals.Count(r => r.Status == ReferralStatus.Pending),
            CreditsEarned = referrals.Count(r => r.Status == ReferralStatus.Converted) * AppConstants.ReferralRewardAmount
        };
    }

    private static string GenerateUniqueCode()
    {
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }
}
