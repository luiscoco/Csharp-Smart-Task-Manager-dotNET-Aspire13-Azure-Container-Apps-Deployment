using SmartTaskManager.Application.Abstractions.Notifications;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Infrastructure.Notifications;

public sealed class SilentNotificationService : INotificationService
{
    public void Notify(string message)
    {
    }

    public void NotifyTaskCompleted(BaseTask task)
    {
    }

    public void NotifyTaskArchived(BaseTask task)
    {
    }
}
