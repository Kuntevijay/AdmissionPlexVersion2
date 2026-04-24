using AdmissionPlex.Core.Entities.Chat;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IChatRepository : IRepository<CareerChatSession>
{
    Task<IEnumerable<CareerChatSession>> GetByStudentIdAsync(long studentId);
    Task<CareerChatSession?> GetWithMessagesAsync(long id);
}
