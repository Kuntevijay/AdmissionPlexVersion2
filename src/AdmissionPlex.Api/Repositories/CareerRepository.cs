using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class CareerRepository : Repository<Career>, ICareerRepository
{
    public CareerRepository(AppDbContext context) : base(context) { }

    public async Task<Career?> GetBySlugAsync(string slug)
        => await _dbSet
            .Include(c => c.Stream)
            .Include(c => c.Subjects)
            .FirstOrDefaultAsync(c => c.Slug == slug);

    public async Task<Career?> GetWithWeightsAsync(long id)
        => await _dbSet
            .Include(c => c.Stream)
            .Include(c => c.InterestWeights).ThenInclude(w => w.InterestCategory)
            .Include(c => c.AptitudeWeights).ThenInclude(w => w.AptitudeCategory)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<Career>> GetAllWithWeightsAsync()
        => await _dbSet
            .Include(c => c.Stream)
            .Include(c => c.InterestWeights)
            .Include(c => c.AptitudeWeights)
            .Where(c => c.IsPublished)
            .ToListAsync();

    public async Task<IEnumerable<Career>> GetPublishedAsync()
        => await _dbSet
            .Include(c => c.Stream)
            .Where(c => c.IsPublished)
            .OrderBy(c => c.Title)
            .ToListAsync();
}
