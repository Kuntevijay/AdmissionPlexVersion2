using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Referrals;

public class ReferralReward : BaseEntity
{
    public long ReferralId { get; set; }
    public long UserId { get; set; }
    public RewardType RewardType { get; set; }
    public decimal Amount { get; set; }
    public RewardStatus Status { get; set; } = RewardStatus.Pending;
    public DateTime? CreditedAt { get; set; }

    public Referral Referral { get; set; } = null!;
}
