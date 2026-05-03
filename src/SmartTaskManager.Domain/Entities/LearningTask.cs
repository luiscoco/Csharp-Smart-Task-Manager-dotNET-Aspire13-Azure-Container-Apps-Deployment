using System;
using System.Collections.Generic;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Domain.Entities;

public sealed class LearningTask : BaseTask
{
    public LearningTask(
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
            category ?? Category.CreateLearningDefault(),
            TaskStatus.Pending)
    {
    }

    public LearningTask(
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

    public override string TaskType => "Learning";

    protected override string BuildCompletedMessage()
    {
        return "Learning task completed and study progress recorded.";
    }
}
