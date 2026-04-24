using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Payments;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class PaymentRepository : Repository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public async Task<Payment?> GetByOrderIdAsync(string orderId)
        => await _dbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);

    public async Task<Payment?> GetByUuidAsync(Guid uuid)
        => await _dbSet.FirstOrDefaultAsync(p => p.Uuid == uuid);

    public async Task<IEnumerable<Payment>> GetByUserIdAsync(long userId)
        => await _dbSet
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
}
