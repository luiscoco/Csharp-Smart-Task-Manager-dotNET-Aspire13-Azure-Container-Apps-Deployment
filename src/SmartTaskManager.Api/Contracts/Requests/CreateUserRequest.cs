using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Api.Contracts.Requests;

public sealed class CreateUserRequest : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string UserName { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            yield return new ValidationResult(
                "UserName is required.",
                new[] { nameof(UserName) });
        }
    }
}
