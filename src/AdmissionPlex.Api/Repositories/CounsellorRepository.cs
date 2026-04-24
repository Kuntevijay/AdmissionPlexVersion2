using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Users;
using AdmissionPlex.Core.Entities.Counselling;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class CounsellorRepository : Repository<CounsellorProfile>, ICounsellorRepository
{
    public CounsellorRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<CounsellorProfile>> GetAvailableAsync()
        => await _dbSet
            .Where(c => c.IsAvailable)
            .OrderByDescending(c => c.Rating)
            .ToListAsync();

    public async Task<IEnumerable<CounsellorAvailability>> GetAvailabilityAsync(long counsellorId)
        => await _context.CounsellorAvailabilities
            .Where(a => a.CounsellorId == counsellorId && a.IsAvailable)
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartTime)
            .ToListAsync();

    public async Task<IEnumerable<CounsellorSession>> GetSessionsByStudentAsync(long studentId)
        => await _context.CounsellorSessions
            .Include(s => s.Counsellor)
            .Where(s => s.StudentId == studentId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();

    public async Task<IEnumerable<CounsellorSession>> GetSessionsByCounsellorAsync(long counsellorId)
        => await _context.CounsellorSessions
            .Include(s => s.Student)
            .Where(s => s.CounsellorId == counsellorId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync();
}
