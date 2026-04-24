using AdmissionPlex.Core.Entities.Careers;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface ICareerRepository : IRepository<Career>
{
    Task<Career?> GetBySlugAsync(string slug);
    Task<Career?> GetWithWeightsAsync(long id);
    Task<IEnumerable<Career>> GetAllWithWeightsAsync();
    Task<IEnumerable<Career>> GetPublishedAsync();
}
