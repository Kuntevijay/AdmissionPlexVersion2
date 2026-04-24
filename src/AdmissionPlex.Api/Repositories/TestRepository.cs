using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class TestRepository : Repository<Test>, ITestRepository
{
    public TestRepository(AppDbContext context) : base(context) { }

    public async Task<Test?> GetBySlugAsync(string slug)
        => await _dbSet.FirstOrDefaultAsync(t => t.Slug == slug);

    public async Task<Test?> GetWithSectionsAsync(long id)
        => await _dbSet
            .Include(t => t.Sections.OrderBy(s => s.SectionOrder))
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Test?> GetWithFullDetailsAsync(long id)
        => await _dbSet
            .Include(t => t.Sections.OrderBy(s => s.SectionOrder))
                .ThenInclude(s => s.Questions.OrderBy(q => q.QuestionOrder))
                    .ThenInclude(sq => sq.Question)
                        .ThenInclude(q => q.Options.OrderBy(o => o.OptionOrder))
            .Include(t => t.Sections)
                .ThenInclude(s => s.InterestCategory)
            .Include(t => t.Sections)
                .ThenInclude(s => s.AptitudeCategory)
            .FirstOrDefaultAsync(t => t.Id == id);
}
