using AdmissionPlex.Core.Entities.Tests;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface ITestAttemptRepository : IRepository<TestAttempt>
{
    Task<TestAttempt?> GetByUuidAsync(Guid uuid);
    Task<TestAttempt?> GetWithResponsesAsync(long id);
    Task<TestAttempt?> GetWithFullResultsAsync(long id);
    Task<IEnumerable<TestAttempt>> GetByStudentIdAsync(long studentId);
}
