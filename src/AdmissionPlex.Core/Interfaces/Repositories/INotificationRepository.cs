using AdmissionPlex.Core.Entities.Notifications;

namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(long userId, bool unreadOnly = false);
    Task MarkAsReadAsync(long id);
    Task MarkAllAsReadAsync(long userId);
}
