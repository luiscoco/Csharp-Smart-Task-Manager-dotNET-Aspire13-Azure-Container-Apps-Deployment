using System;
using System.Collections.Generic;
using System.Linq;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Application.Filters;

public sealed class StatusTaskFilter
{
    public IReadOnlyCollection<BaseTask> Apply(IEnumerable<BaseTask> tasks, TaskStatus status)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return tasks
            .Where(task => task.Status == status)
            .ToList();
    }
}
