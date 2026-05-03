using System;
using System.Collections.Generic;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Domain.Entities;

public sealed class WorkTask : BaseTask
{
    public WorkTask(
        Guid userId,
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        Category? category = null)
        : this(
            Guid.NewGuid(),
            userId,
            title,
            description,
            dueDate,
            priority,
            category ?? Category.CreateWorkDefault(),
            TaskStatus.Pending)
    {
    }

    public WorkTask(
        Guid id,
        Guid userId,
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        Category category,
        TaskStatus status,
        IEnumerable<HistoryEntry>? history = null)
        : base(id, userId, title, description, dueDate, priority, category, status, history)
    {
    }

    public override string TaskType => "Work";

    protected override string BuildPriorityChangedMessage(TaskPriority previousPriority, TaskPriority newPriority)
    {
        return $"Work task priority updated from {previousPriority} to {newPriority}.";
    }
}
