using System;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Application.DTOs;

public sealed record TaskSummary(
    Guid Id,
    string Title,
    string Description,
    DateTime DueDate,
    TaskPriority Priority,
    TaskStatus Status,
    string CategoryName,
    string TaskType,
    bool IsOverdue,
    int HistoryCount)
{
    public static TaskSummary FromTask(BaseTask task, DateTime referenceDate)
    {
        ArgumentNullException.ThrowIfNull(task);

        return new TaskSummary(
            task.Id,
            task.Title,
            task.Description,
            task.DueDate,
            task.Priority,
            task.Status,
            task.Category.Name,
            task.TaskType,
            task.IsOverdue(referenceDate),
            task.History.Count);
    }
}
