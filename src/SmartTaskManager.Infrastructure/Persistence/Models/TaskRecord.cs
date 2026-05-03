using System;
using System.Collections.Generic;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Infrastructure.Persistence.Models;

public sealed class TaskRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TaskType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public TaskPriority Priority { get; set; }

    public TaskStatus Status { get; set; }

    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string CategoryDescription { get; set; } = string.Empty;

    public List<TaskHistoryRecord> HistoryEntries { get; set; } = new();
}
