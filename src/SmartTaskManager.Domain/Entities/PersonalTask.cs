using System;
using System.Collections.Generic;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Domain.Entities;

public sealed class PersonalTask : BaseTask
{
    public PersonalTask(
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
            category ?? Category.CreatePersonalDefault(),
            TaskStatus.Pending)
    {
    }

    public PersonalTask(
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

    public override string TaskType => "Personal";

    protected override string BuildCompletedMessage()
    {
        return "Personal task completed and daily plan updated.";
    }
}
