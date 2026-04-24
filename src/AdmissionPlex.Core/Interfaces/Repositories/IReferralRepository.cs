using AdmissionPlex.Core.Entities.Referrals;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IReferralRepository : IRepository<Referral>
{
    Task<ReferralCode?> GetCodeByUserIdAsync(long userId);
    Task<ReferralCode?> GetCodeByCodeAsync(string code);
    Task<IEnumerable<Referral>> GetByReferrerAsync(long userId);
}
