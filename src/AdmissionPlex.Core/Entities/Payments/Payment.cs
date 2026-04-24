using AdmissionPlex.Core.Common;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Entities.Payments;

public class Payment : AuditableEntity
{
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long UserId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public PaymentFor PaymentFor { get; set; }
    public long? ReferenceId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public string? CcavenueTrackingId { get; set; }
    public string? CcavenueBankRefNo { get; set; }
    public string? CcavenueOrderStatus { get; set; }
    public string? PaymentMode { get; set; }
    public string? CardName { get; set; }
    public string? StatusMessage { get; set; }
    public string? CcavenueResponseJson { get; set; }
    public string? DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime? PaidAt { get; set; }

}
