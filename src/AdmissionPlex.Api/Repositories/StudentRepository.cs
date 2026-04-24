using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Users;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class StudentRepository : Repository<StudentProfile>, IStudentRepository
{
    public StudentRepository(AppDbContext context) : base(context) { }

    public async Task<StudentProfile?> GetByUserIdAsync(long userId)
        => await _dbSet
            .FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<IEnumerable<StudentProfile>> GetByCoordinatorIdAsync(long coordinatorId)
        => await _dbSet
            .Where(s => s.CoordinatorId == coordinatorId)
            .OrderBy(s => s.LastName)
            .ToListAsync();
}
