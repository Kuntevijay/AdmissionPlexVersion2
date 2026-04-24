using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class QuestionRepository : Repository<Question>, IQuestionRepository
{
    public QuestionRepository(AppDbContext context) : base(context) { }

    private IQueryable<Question> FullQuery => _dbSet
        .Include(q => q.Options)
        .Include(q => q.InterestCategory)
        .Include(q => q.AptitudeCategory);

    public async Task<IEnumerable<Question>> GetBySectionTypeAsync(SectionType sectionType)
        => await FullQuery
            .Where(q => q.SectionType == sectionType && q.IsActive)
            .OrderBy(q => q.Id)
            .ToListAsync();

    public async Task<IEnumerable<Question>> GetByInterestCategoryAsync(long interestCategoryId)
        => await FullQuery
            .Where(q => q.InterestCategoryId == interestCategoryId && q.IsActive)
            .ToListAsync();

    public async Task<IEnumerable<Question>> GetByAptitudeCategoryAsync(long aptitudeCategoryId)
        => await FullQuery
            .Where(q => q.AptitudeCategoryId == aptitudeCategoryId && q.IsActive)
            .ToListAsync();

    public async Task<Question?> GetWithOptionsAsync(long id)
        => await FullQuery
            .FirstOrDefaultAsync(q => q.Id == id);

    public new async Task<IEnumerable<Question>> GetAllAsync()
        => await FullQuery.OrderBy(q => q.Id).ToListAsync();

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
