using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Content;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class PageRepository : Repository<Page>, IPageRepository
{
    public PageRepository(AppDbContext context) : base(context) { }

    public async Task<Page?> GetBySlugAsync(string slug)
        => await _dbSet.FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

    public async Task<IEnumerable<Page>> GetPublishedAsync()
        => await _dbSet
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .ToListAsync();

    public async Task<IEnumerable<Faq>> GetPublishedFaqsAsync()
        => await _context.Faqs
            .Where(f => f.IsPublished)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.DisplayOrder)
            .ToListAsync();
}
