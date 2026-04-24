using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Cutoffs;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class CutoffRepository : Repository<CutoffData>, ICutoffRepository
{
    public CutoffRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<CutoffData>> SearchAsync(
        ExamType? exam, int? year, long? collegeId, long? branchId, string? category)
    {
        var query = _dbSet
            .Include(c => c.College)
            .Include(c => c.Branch)
            .AsQueryable();

        if (exam.HasValue) query = query.Where(c => c.Exam == exam.Value);
        if (year.HasValue) query = query.Where(c => c.Year == year.Value);
        if (collegeId.HasValue) query = query.Where(c => c.CollegeId == collegeId.Value);
        if (branchId.HasValue) query = query.Where(c => c.BranchId == branchId.Value);
        if (!string.IsNullOrEmpty(category)) query = query.Where(c => c.Category == category);

        return await query
            .OrderBy(c => c.College.Name)
            .ThenBy(c => c.Branch.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<College>> GetAllCollegesAsync()
        => await _context.Colleges
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<IEnumerable<Branch>> GetAllBranchesAsync()
        => await _context.Branches
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();
}
