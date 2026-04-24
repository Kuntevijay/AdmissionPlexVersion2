using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Interfaces.Services;

public interface ICutoffSearchService
{
    Task<object> SearchCutoffsAsync(ExamType? exam, int? year, long? collegeId, long? branchId, string? category, string? region);
}
