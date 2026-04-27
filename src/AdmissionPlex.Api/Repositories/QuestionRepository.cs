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

    public async Task<(IEnumerable<Question> Items, int TotalCount)> SearchAsync(
        string? search,
        SectionType? sectionType,
        long? interestCategoryId,
        long? aptitudeCategoryId,
        bool activeOnly,
        int page,
        int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        var q = FullQuery;
        if (activeOnly) q = q.Where(x => x.IsActive);
        if (sectionType.HasValue) q = q.Where(x => x.SectionType == sectionType.Value);
        if (interestCategoryId.HasValue) q = q.Where(x => x.InterestCategoryId == interestCategoryId.Value);
        if (aptitudeCategoryId.HasValue) q = q.Where(x => x.AptitudeCategoryId == aptitudeCategoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                EF.Functions.ILike(x.QuestionText, $"%{s}%") ||
                (x.InterestCategory != null && EF.Functions.ILike(x.InterestCategory.Name, $"%{s}%")) ||
                (x.AptitudeCategory != null && EF.Functions.ILike(x.AptitudeCategory.Name, $"%{s}%")));
        }

        var totalCount = await q.CountAsync();
        var items = await q.OrderBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<QuestionStats> GetStatsAsync(bool activeOnly)
    {
        var q = _dbSet.AsQueryable();
        if (activeOnly) q = q.Where(x => x.IsActive);

        var perSection = await q
            .GroupBy(x => x.SectionType)
            .Select(g => new { Section = g.Key, Count = g.Count() })
            .ToListAsync();

        var perInterest = await q
            .Where(x => x.SectionType == SectionType.Interest && x.InterestCategory != null)
            .GroupBy(x => x.InterestCategory!.Code)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToListAsync();

        var perAptitude = await q
            .Where(x => x.SectionType == SectionType.Aptitude && x.AptitudeCategory != null)
            .GroupBy(x => x.AptitudeCategory!.Code)
            .Select(g => new { Code = g.Key, Count = g.Count() })
            .ToListAsync();

        var stats = new QuestionStats
        {
            StreamSelector = perSection.FirstOrDefault(x => x.Section == SectionType.StreamSelector)?.Count ?? 0,
            Interest = perSection.FirstOrDefault(x => x.Section == SectionType.Interest)?.Count ?? 0,
            Aptitude = perSection.FirstOrDefault(x => x.Section == SectionType.Aptitude)?.Count ?? 0,
            InterestByCode = perInterest.ToDictionary(x => x.Code, x => x.Count),
            AptitudeByCode = perAptitude.ToDictionary(x => x.Code, x => x.Count)
        };
        stats.Total = stats.StreamSelector + stats.Interest + stats.Aptitude;
        return stats;
    }
}
