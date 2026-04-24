using AdmissionPlex.Core.Entities.Content;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IPageRepository : IRepository<Page>
{
    Task<Page?> GetBySlugAsync(string slug);
    Task<IEnumerable<Page>> GetPublishedAsync();
    Task<IEnumerable<Faq>> GetPublishedFaqsAsync();
}
