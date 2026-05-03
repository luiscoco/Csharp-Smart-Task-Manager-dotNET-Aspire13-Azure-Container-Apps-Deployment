using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Web.Models.Forms;

public sealed class CreateTaskFormModel : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateOnly? DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(2));

    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [StringLength(100)]
    public string? CategoryName { get; set; }

    [Required]
    public TaskKind TaskType { get; set; } = TaskKind.Work;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult(
                "Title is required.",
                new[] { nameof(Title) });
        }

        if (!DueDate.HasValue)
        {
            yield return new ValidationResult(
                "Due date is required.",
                new[] { nameof(DueDate) });
        }
        else if (DueDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Due date cannot be in the past.",
                new[] { nameof(DueDate) });
        }

        if (CategoryName is not null && string.IsNullOrWhiteSpace(CategoryName))
        {
            yield return new ValidationResult(
                "Category cannot be empty when provided.",
                new[] { nameof(CategoryName) });
        }
    }
}
