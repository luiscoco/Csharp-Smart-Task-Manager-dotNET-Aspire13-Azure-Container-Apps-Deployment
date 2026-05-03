using System;
using System.Collections.Generic;
using SmartTaskManager.Domain.Common;
using SmartTaskManager.Domain.Enums;
using SmartTaskManager.Domain.Records;

namespace SmartTaskManager.Domain.Entities;

public abstract class BaseTask : BaseEntity
{
    private readonly List<HistoryEntry> _history = new();

    protected BaseTask(
        Guid id,
        Guid userId,
        string title,
        string description,
        DateTime dueDate,
        TaskPriority priority,
        Category category,
        TaskStatus status = TaskStatus.Pending,
        IEnumerable<HistoryEntry>? history = null)
        : base(id)
    {
        UserId = ValidateUserId(userId);
        Title = ValidateTitle(title);
        Description = NormalizeDescription(description);
        DueDate = ValidateDueDate(dueDate);
        Priority = ValidatePriority(priority);
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Status = ValidateStatus(status);

        if (history is not null)
        {
            _history.AddRange(history);
        }

        if (_history.Count == 0)
        {
            AddHistoryEntry("Created", BuildCreatedMessage());
        }
    }

    public Guid UserId { get; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateTime DueDate { get; private set; }

    public TaskPriority Priority { get; private set; }

    public TaskStatus Status { get; private set; }

    public Category Category { get; private set; }

    public IReadOnlyCollection<HistoryEntry> History => _history.AsReadOnly();

    public abstract string TaskType { get; }

    public void ChangePriority(TaskPriority newPriority)
    {
        EnsureActiveForModification();

        TaskPriority validatedPriority = ValidatePriority(newPriority);
        if (Priority == validatedPriority)
        {
            return;
        }

        TaskPriority previousPriority = Priority;
        Priority = validatedPriority;

        AddHistoryEntry(
            "PriorityChanged",
            BuildPriorityChangedMessage(previousPriority, validatedPriority));
    }

    public void MarkAsCompleted()
    {
        if (Status == TaskStatus.Completed)
        {
            return;
        }

        if (Status == TaskStatus.Archived)
        {
            throw new DomainException("Archived tasks cannot be marked as completed.");
        }

        Status = TaskStatus.Completed;
        AddHistoryEntry("Completed", BuildCompletedMessage());
    }

    public void Archive()
    {
        if (Status == TaskStatus.Archived)
        {
            return;
        }

        Status = TaskStatus.Archived;
        AddHistoryEntry("Archived", BuildArchivedMessage());
    }

    protected void AddHistoryEntry(string action, string details)
    {
        _history.Add(HistoryEntry.Create(action, details));
    }

    public bool IsOverdue(DateTime referenceDate)
    {
        return DueDate < referenceDate
            && Status is not TaskStatus.Completed
            && Status is not TaskStatus.Archived;
    }

    protected virtual string BuildCreatedMessage()
    {
        return $"{TaskType} task created in category '{Category.Name}'.";
    }

    protected virtual string BuildPriorityChangedMessage(TaskPriority previousPriority, TaskPriority newPriority)
    {
        return $"{TaskType} task priority changed from {previousPriority} to {newPriority}.";
    }

    protected virtual string BuildCompletedMessage()
    {
        return $"{TaskType} task marked as completed.";
    }

    protected virtual string BuildArchivedMessage()
    {
        return $"{TaskType} task archived.";
    }

    private void EnsureActiveForModification()
    {
        if (Status == TaskStatus.Completed)
        {
            throw new DomainException("Completed tasks cannot be modified.");
        }

        if (Status == TaskStatus.Archived)
        {
            throw new DomainException("Archived tasks cannot be modified.");
        }
    }

    private static Guid ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("A task must belong to a valid user.");
        }

        return userId;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Task title is required.");
        }

        return title.Trim();
    }

    private static string NormalizeDescription(string description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : description.Trim();
    }

    private static DateTime ValidateDueDate(DateTime dueDate)
    {
        if (dueDate == default)
        {
            throw new DomainException("Task due date is required.");
        }

        return dueDate;
    }

    private static TaskPriority ValidatePriority(TaskPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new DomainException("Task priority is invalid.");
        }

        return priority;
    }

    private static TaskStatus ValidateStatus(TaskStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Task status is invalid.");
        }

        return status;
    }
}
