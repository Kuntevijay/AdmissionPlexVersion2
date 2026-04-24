using AdmissionPlex.Core.Entities.Payments;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(string orderId);
    Task<Payment?> GetByUuidAsync(Guid uuid);
    Task<IEnumerable<Payment>> GetByUserIdAsync(long userId);
}
