using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Chat;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class ChatRepository : Repository<CareerChatSession>, IChatRepository
{
    public ChatRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<CareerChatSession>> GetByStudentIdAsync(long studentId)
        => await _dbSet
            .Where(s => s.StudentId == studentId && s.IsActive)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync();

    public async Task<CareerChatSession?> GetWithMessagesAsync(long id)
        => await _dbSet
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id);
}
