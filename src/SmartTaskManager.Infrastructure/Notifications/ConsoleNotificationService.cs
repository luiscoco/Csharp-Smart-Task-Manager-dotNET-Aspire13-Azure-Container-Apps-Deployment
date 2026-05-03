using System;
using System.IO;
using SmartTaskManager.Application.Abstractions.Notifications;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Infrastructure.Notifications;

public sealed class ConsoleNotificationService : INotificationService
{
    private readonly TextWriter _writer;

    public ConsoleNotificationService()
        : this(Console.Out)
    {
    }

    public ConsoleNotificationService(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void Notify(string message)
    {
        WriteMessage("INFO", message);
    }

    public void NotifyTaskCompleted(BaseTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        WriteMessage("SUCCESS", $"Task '{task.Title}' was marked as completed.");
    }

    public void NotifyTaskArchived(BaseTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        WriteMessage("INFO", $"Task '{task.Title}' was archived.");
    }

    private void WriteMessage(string level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _writer.WriteLine($"{DateTime.Now:HH:mm:ss} [{level}] {message}");
    }
}
