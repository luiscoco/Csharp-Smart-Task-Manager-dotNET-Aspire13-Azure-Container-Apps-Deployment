using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Web.Options;

public sealed class AzureAdOptions
{
    public const string SectionName = "AzureAd";
    public const string ClientSecretPlaceholder = "__SET_IN_USER_SECRETS_OR_ENVIRONMENT__";

    [Required]
    [Url]
    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    [Required]
    public string TenantId { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = ClientSecretPlaceholder;

    [Required]
    public string CallbackPath { get; init; } = "/signin-oidc";

    [Required]
    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";
}
