using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Api.Contracts.Requests;

public sealed class TaskListQueryRequest : IValidatableObject
{
    [EnumDataType(typeof(TaskStatus))]
    public TaskStatus? Status { get; init; }

    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority? Priority { get; init; }

    public bool Overdue { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        int selectedFilters = 0;

        if (Status.HasValue)
        {
            selectedFilters++;
        }

        if (Priority.HasValue)
        {
            selectedFilters++;
        }

        if (Overdue)
        {
            selectedFilters++;
        }

        if (selectedFilters <= 1)
        {
            yield break;
        }

        yield return new ValidationResult(
            "Use only one task filter at a time: status, priority, or overdue.",
            new[] { nameof(Status), nameof(Priority), nameof(Overdue) });
    }
}
