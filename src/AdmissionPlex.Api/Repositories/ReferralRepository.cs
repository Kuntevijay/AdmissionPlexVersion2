using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Referrals;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class ReferralRepository : Repository<Referral>, IReferralRepository
{
    public ReferralRepository(AppDbContext context) : base(context) { }

    public async Task<ReferralCode?> GetCodeByUserIdAsync(long userId)
        => await _context.ReferralCodes
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);

    public async Task<ReferralCode?> GetCodeByCodeAsync(string code)
        => await _context.ReferralCodes
            .FirstOrDefaultAsync(r => r.Code == code && r.IsActive);

    public async Task<IEnumerable<Referral>> GetByReferrerAsync(long userId)
        => await _dbSet
            .Include(r => r.ReferralCode)
            .Where(r => r.ReferrerUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}
