using AdmissionPlex.Core.Entities.Users;
using AdmissionPlex.Core.Entities.Counselling;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface ICounsellorRepository : IRepository<CounsellorProfile>
{
    Task<IEnumerable<CounsellorProfile>> GetAvailableAsync();
    Task<IEnumerable<CounsellorAvailability>> GetAvailabilityAsync(long counsellorId);
    Task<IEnumerable<CounsellorSession>> GetSessionsByStudentAsync(long studentId);
    Task<IEnumerable<CounsellorSession>> GetSessionsByCounsellorAsync(long counsellorId);
}
