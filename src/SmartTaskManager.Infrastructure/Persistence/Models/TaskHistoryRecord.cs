using System;

namespace SmartTaskManager.Infrastructure.Persistence.Models;

public sealed class TaskHistoryRecord
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public int Sequence { get; set; }

    public DateTime OccurredOnUtc { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}
