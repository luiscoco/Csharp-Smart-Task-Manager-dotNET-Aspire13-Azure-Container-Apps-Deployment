using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Web.Models.Forms;

public sealed class CreateUserFormModel : IValidatableObject
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string UserName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            yield return new ValidationResult(
                "User name is required.",
                new[] { nameof(UserName) });
        }
    }
}
