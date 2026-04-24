using AdmissionPlex.Core.Common;

namespace AdmissionPlex.Core.Entities.Referrals;

public class ReferralCode : BaseEntity
{
    public long UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? MaxUses { get; set; }
    public int TimesUsed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
