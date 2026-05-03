using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Web.Options;

public sealed class SmartTaskManagerApiOptions
{
    public const string SectionName = "SmartTaskManagerApi";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://localhost:7081/";

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    public string Scopes { get; init; } = string.Empty;
}
