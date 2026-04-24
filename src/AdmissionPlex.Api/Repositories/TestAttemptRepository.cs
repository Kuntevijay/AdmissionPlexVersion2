using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class TestAttemptRepository : Repository<TestAttempt>, ITestAttemptRepository
{
    public TestAttemptRepository(AppDbContext context) : base(context) { }

    public async Task<TestAttempt?> GetByUuidAsync(Guid uuid)
        => await _dbSet
            .Include(a => a.Test)
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.Uuid == uuid);

    public async Task<TestAttempt?> GetWithResponsesAsync(long id)
        => await _dbSet
            .Include(a => a.Responses)
                .ThenInclude(r => r.Question)
            .Include(a => a.Responses)
                .ThenInclude(r => r.SelectedOption)
            .Include(a => a.Test)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<TestAttempt?> GetWithFullResultsAsync(long id)
        => await _dbSet
            .Include(a => a.Student)
            .Include(a => a.Test)
            .Include(a => a.InterestScores).ThenInclude(s => s.InterestCategory)
            .Include(a => a.AptitudeScores).ThenInclude(s => s.AptitudeCategory)
            .Include(a => a.CareerSuitabilityScores).ThenInclude(s => s.Career).ThenInclude(c => c.Stream)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<TestAttempt>> GetByStudentIdAsync(long studentId)
        => await _dbSet
            .Include(a => a.Test)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
}
