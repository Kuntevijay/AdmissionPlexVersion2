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

    Task<(IEnumerable<Question> Items, int TotalCount)> SearchAsync(
        string? search,
        SectionType? sectionType,
        long? interestCategoryId,
        long? aptitudeCategoryId,
        bool activeOnly,
        int page,
        int pageSize);

    Task<QuestionStats> GetStatsAsync(bool activeOnly);
}

public class QuestionStats
{
    public int Total { get; set; }
    public int StreamSelector { get; set; }
    public int Interest { get; set; }
    public int Aptitude { get; set; }
    public Dictionary<string, int> InterestByCode { get; set; } = new();
    public Dictionary<string, int> AptitudeByCode { get; set; } = new();
}
