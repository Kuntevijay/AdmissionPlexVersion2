using AdmissionPlex.Core.Entities.Users;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IStudentRepository : IRepository<StudentProfile>
{
    Task<StudentProfile?> GetByUserIdAsync(long userId);
    Task<IEnumerable<StudentProfile>> GetByCoordinatorIdAsync(long coordinatorId);
}
