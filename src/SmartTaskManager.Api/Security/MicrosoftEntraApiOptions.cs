using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartTaskManager.Api.Security;

public sealed class MicrosoftEntraApiOptions
{
    public const string SectionName = "AzureAd";

    /// <summary>
    /// Well-known tenant ID used by personal Microsoft accounts (MSA) in v2.0 tokens.
    /// </summary>
    private const string MsaConsumerTenantId = "9188040d-6c67-4c5b-b112-36a304b66dad";

    public string Instance { get; init; } = "https://login.microsoftonline.com/";

    public string TenantId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Use the "common" authority so the OIDC metadata endpoint returns signing keys
    /// for both organizational and personal Microsoft accounts.
    /// </summary>
    public string Authority => $"{Instance.TrimEnd('/')}/common/v2.0";

    /// <summary>
    /// The original single-tenant authority, kept for reference and valid-issuer matching.
    /// </summary>
    public string TenantAuthority => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";

    public string EffectiveAudience => string.IsNullOrWhiteSpace(Audience)
        ? $"api://{ClientId}"
        : Audience;

    public IReadOnlyCollection<string> ValidAudiences => new[]
        {
            EffectiveAudience,
            ClientId
        }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>
    /// Returns all issuers the API should accept:
    /// the configured organization tenant and the MSA consumer tenant.
    /// </summary>
    public IReadOnlyCollection<string> ValidIssuers => new[]
        {
            $"{Instance.TrimEnd('/')}/{TenantId}/v2.0",
            $"{Instance.TrimEnd('/')}/{MsaConsumerTenantId}/v2.0"
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
