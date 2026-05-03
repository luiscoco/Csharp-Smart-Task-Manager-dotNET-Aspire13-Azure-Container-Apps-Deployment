namespace SmartTaskManager.Web.Models;

public sealed record TaskItem(
    Guid Id,
    string Title,
    string Description,
    DateTime DueDate,
    TaskPriority Priority,
    TaskStatus Status,
    string CategoryName,
    string TaskType,
    bool IsOverdue,
    int HistoryCount);
