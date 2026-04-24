using AdmissionPlex.Core.Entities.Tests;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface ITestRepository : IRepository<Test>
{
    Task<Test?> GetBySlugAsync(string slug);
    Task<Test?> GetWithSectionsAsync(long id);
    Task<Test?> GetWithFullDetailsAsync(long id);
}
