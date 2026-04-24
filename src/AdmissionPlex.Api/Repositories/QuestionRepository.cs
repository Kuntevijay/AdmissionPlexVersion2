using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class QuestionRepository : Repository<Question>, IQuestionRepository
{
    public QuestionRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Question>> GetBySectionTypeAsync(SectionType sectionType)
        => await _dbSet
            .Include(q => q.Options)
            .Where(q => q.SectionType == sectionType && q.IsActive)
            .OrderBy(q => q.Id)
            .ToListAsync();

    public async Task<IEnumerable<Question>> GetByInterestCategoryAsync(long interestCategoryId)
        => await _dbSet
            .Include(q => q.Options)
            .Where(q => q.InterestCategoryId == interestCategoryId && q.IsActive)
            .ToListAsync();

    public async Task<IEnumerable<Question>> GetByAptitudeCategoryAsync(long aptitudeCategoryId)
        => await _dbSet
            .Include(q => q.Options)
            .Where(q => q.AptitudeCategoryId == aptitudeCategoryId && q.IsActive)
            .ToListAsync();

    public async Task<Question?> GetWithOptionsAsync(long id)
        => await _dbSet
            .Include(q => q.Options.OrderBy(o => o.OptionOrder))
            .Include(q => q.InterestCategory)
            .Include(q => q.AptitudeCategory)
            .FirstOrDefaultAsync(q => q.Id == id);

    public async Task<IEnumerable<InterestCategory>> GetAllInterestCategoriesAsync()
        => await _context.InterestCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

    public async Task<IEnumerable<AptitudeCategory>> GetAllAptitudeCategoriesAsync()
        => await _context.AptitudeCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
}
