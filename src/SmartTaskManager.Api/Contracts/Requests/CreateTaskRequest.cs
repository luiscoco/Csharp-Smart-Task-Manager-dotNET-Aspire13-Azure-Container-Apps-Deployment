using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartTaskManager.Api.Contracts;
using SmartTaskManager.Domain.Enums;

namespace SmartTaskManager.Api.Contracts.Requests;

public sealed class CreateTaskRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required]
    public DateTime? DueDate { get; init; }

    [Required]
    [EnumDataType(typeof(TaskPriority))]
    public TaskPriority? Priority { get; init; }

    [StringLength(100)]
    public string? CategoryName { get; init; }

    [Required]
    [EnumDataType(typeof(TaskKind))]
    public TaskKind? TaskType { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult(
                "Title is required.",
                new[] { nameof(Title) });
        }

        if (DueDate.HasValue && DueDate.Value == default)
        {
            yield return new ValidationResult(
                "DueDate is required.",
                new[] { nameof(DueDate) });
        }

        if (CategoryName is not null && string.IsNullOrWhiteSpace(CategoryName))
        {
            yield return new ValidationResult(
                "CategoryName cannot be empty when provided.",
                new[] { nameof(CategoryName) });
        }
    }
}
