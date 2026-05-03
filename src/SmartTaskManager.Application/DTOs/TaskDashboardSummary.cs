using System;
using System.Collections.Generic;
using System.Linq;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Application.DTOs;

public sealed record TaskDashboardSummary(
    int TotalTasks,
    int PendingTasks,
    int InProgressTasks,
    int CompletedTasks,
    int ArchivedTasks,
    int OverdueTasks)
{
    public static TaskDashboardSummary FromTasks(IEnumerable<BaseTask> tasks, DateTime referenceDate)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        BaseTask[] taskArray = tasks.ToArray();

        return new TaskDashboardSummary(
            taskArray.Length,
            taskArray.Count(task => task.Status == TaskStatus.Pending),
            taskArray.Count(task => task.Status == TaskStatus.InProgress),
            taskArray.Count(task => task.Status == TaskStatus.Completed),
            taskArray.Count(task => task.Status == TaskStatus.Archived),
            taskArray.Count(task => task.IsOverdue(referenceDate)));
    }
}
