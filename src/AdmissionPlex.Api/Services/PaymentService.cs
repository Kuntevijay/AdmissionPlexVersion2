using System.Text.Json;
using AdmissionPlex.Core.Entities.Payments;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Core.Interfaces.Services;

namespace AdmissionPlex.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly ICCavenueService _ccavenue;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork uow, ICCavenueService ccavenue, IConfiguration config, ILogger<PaymentService> logger)
    {
        _uow = uow;
        _ccavenue = ccavenue;
        _config = config;
        _logger = logger;
    }

    public async Task<Payment> InitiatePaymentAsync(long userId, decimal amount, string paymentFor, long? referenceId)
    {
        if (!Enum.TryParse<PaymentFor>(paymentFor, true, out var pf))
            throw new ArgumentException("Invalid payment type.");

        var orderId = $"AP{DateTime.UtcNow:yyyyMMddHHmmss}{userId}";

        var payment = new Payment
        {
            UserId = userId,
            OrderId = orderId,
            Amount = amount,
            PaymentFor = pf,
            ReferenceId = referenceId,
            Status = PaymentStatus.Initiated
        };

        await _uow.Payments.AddAsync(payment);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Payment initiated: OrderId={OrderId}, Amount={Amount}, For={For}", orderId, amount, paymentFor);
        return payment;
    }

    public async Task<Payment> ProcessResponseAsync(string encryptedResponse)
    {
        var responseData = _ccavenue.ParseResponse(encryptedResponse);

        var orderId = responseData.GetValueOrDefault("order_id", "");
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId);

        if (payment == null)
            throw new KeyNotFoundException($"Payment not found for order: {orderId}");

        payment.CcavenueTrackingId = responseData.GetValueOrDefault("tracking_id");
        payment.CcavenueBankRefNo = responseData.GetValueOrDefault("bank_ref_no");
        payment.CcavenueOrderStatus = responseData.GetValueOrDefault("order_status");
        payment.PaymentMode = responseData.GetValueOrDefault("payment_mode");
        payment.CardName = responseData.GetValueOrDefault("card_name");
        payment.StatusMessage = responseData.GetValueOrDefault("status_message");
        payment.CcavenueResponseJson = JsonSerializer.Serialize(responseData);

        var orderStatus = responseData.GetValueOrDefault("order_status", "").ToLower();
        payment.Status = orderStatus switch
        {
            "success" => PaymentStatus.Success,
            "failure" => PaymentStatus.Failed,
            "aborted" => PaymentStatus.Aborted,
            _ => PaymentStatus.Invalid
        };

        if (payment.Status == PaymentStatus.Success)
            payment.PaidAt = DateTime.UtcNow;

        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Payment processed: OrderId={OrderId}, Status={Status}", orderId, payment.Status);
        return payment;
    }

    public async Task<Payment> ProcessCancellationAsync(string encryptedResponse)
    {
        var responseData = _ccavenue.ParseResponse(encryptedResponse);
        var orderId = responseData.GetValueOrDefault("order_id", "");
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId);

        if (payment == null)
            throw new KeyNotFoundException($"Payment not found for order: {orderId}");

        payment.Status = PaymentStatus.Aborted;
        payment.StatusMessage = "Payment cancelled by user.";
        payment.CcavenueResponseJson = JsonSerializer.Serialize(responseData);

        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync();

        return payment;
    }
}
