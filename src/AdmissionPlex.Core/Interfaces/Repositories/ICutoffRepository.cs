using AdmissionPlex.Core.Entities.Cutoffs;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface ICutoffRepository : IRepository<CutoffData>
{
    Task<IEnumerable<CutoffData>> SearchAsync(ExamType? exam, int? year, long? collegeId, long? branchId, string? category);
    Task<IEnumerable<College>> GetAllCollegesAsync();
    Task<IEnumerable<Branch>> GetAllBranchesAsync();
}
