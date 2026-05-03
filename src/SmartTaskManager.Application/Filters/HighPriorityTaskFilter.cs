using System;
using System.Collections.Generic;
using System.Linq;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Application.Filters;

public sealed class HighPriorityTaskFilter
{
    public IReadOnlyCollection<BaseTask> Apply(IEnumerable<BaseTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return tasks
            .Where(task => task.Priority is TaskPriority.High or TaskPriority.Critical)
            .ToList();
    }
}
