using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Notifications;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(long userId, bool unreadOnly = false)
    {
        var query = _dbSet.Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);
        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task MarkAsReadAsync(long id)
    {
        var notification = await _dbSet.FindAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
        }
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
