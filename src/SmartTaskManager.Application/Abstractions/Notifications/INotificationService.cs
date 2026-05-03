using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Application.Abstractions.Notifications;

public interface INotificationService
{
    void Notify(string message);

    void NotifyTaskCompleted(BaseTask task);

    void NotifyTaskArchived(BaseTask task);
}
