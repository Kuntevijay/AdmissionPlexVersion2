using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Referrals;

public class Referral : BaseEntity
{
    public long ReferrerUserId { get; set; }
    public long ReferredUserId { get; set; }
    public long ReferralCodeId { get; set; }
    public ReferralStatus Status { get; set; } = ReferralStatus.Pending;
    public DateTime? ConvertedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ReferralCode ReferralCode { get; set; } = null!;
}
