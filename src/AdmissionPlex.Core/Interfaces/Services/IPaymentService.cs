using AdmissionPlex.Core.Entities.Payments;

namespace AdmissionPlex.Core.Interfaces.Services;

public interface IPaymentService
{
    Task<Payment> InitiatePaymentAsync(long userId, decimal amount, string paymentFor, long? referenceId);
    Task<Payment> ProcessResponseAsync(string encryptedResponse);
    Task<Payment> ProcessCancellationAsync(string encryptedResponse);
}
