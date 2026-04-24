using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Enums;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IQuestionRepository : IRepository<Question>
{
    Task<IEnumerable<Question>> GetBySectionTypeAsync(SectionType sectionType);
    Task<IEnumerable<Question>> GetByInterestCategoryAsync(long interestCategoryId);
    Task<IEnumerable<Question>> GetByAptitudeCategoryAsync(long aptitudeCategoryId);
    Task<Question?> GetWithOptionsAsync(long id);
    Task<IEnumerable<InterestCategory>> GetAllInterestCategoriesAsync();
    Task<IEnumerable<AptitudeCategory>> GetAllAptitudeCategoriesAsync();
}
