using System;
using System.Collections.Generic;
using System.Linq;
using SmartTaskManager.Domain.Entities;

namespace SmartTaskManager.Application.Filters;

public sealed class OverdueTaskFilter
{
    public IReadOnlyCollection<BaseTask> Apply(IEnumerable<BaseTask> tasks, DateTime referenceDate)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return tasks
            .Where(task => task.IsOverdue(referenceDate))
            .ToList();
    }
}
