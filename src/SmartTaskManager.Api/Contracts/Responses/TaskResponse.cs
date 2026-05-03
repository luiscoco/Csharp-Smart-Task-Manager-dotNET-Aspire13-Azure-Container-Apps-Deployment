using System;
using System.Collections.Generic;
using System.Linq;
using SmartTaskManager.Application.DTOs;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Api.Contracts.Responses;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime DueDate,
    TaskPriority Priority,
    TaskStatus Status,
    string CategoryName,
    string TaskType,
    bool IsOverdue,
    int HistoryCount)
{
    public static TaskResponse FromApplication(TaskSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new TaskResponse(
            summary.Id,
            summary.Title,
            summary.Description,
            summary.DueDate,
            summary.Priority,
            summary.Status,
            summary.CategoryName,
            summary.TaskType,
            summary.IsOverdue,
            summary.HistoryCount);
    }

    public static IReadOnlyCollection<TaskResponse> FromApplication(IEnumerable<TaskSummary> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        return tasks
            .Select(FromApplication)
            .ToList();
    }
}
