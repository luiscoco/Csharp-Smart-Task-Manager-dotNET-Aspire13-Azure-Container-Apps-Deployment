using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using SmartTaskManager.Web.Options;

namespace SmartTaskManager.Web.Services;

public sealed class SmartTaskManagerApiAccessTokenProvider
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    public SmartTaskManagerApiAccessTokenProvider(
        AuthenticationStateProvider authenticationStateProvider,
        ITokenAcquisition tokenAcquisition,
        IOptions<SmartTaskManagerApiOptions> apiOptions)
    {
        ArgumentNullException.ThrowIfNull(authenticationStateProvider);
        ArgumentNullException.ThrowIfNull(tokenAcquisition);
        ArgumentNullException.ThrowIfNull(apiOptions);

        _authenticationStateProvider = authenticationStateProvider;
        _tokenAcquisition = tokenAcquisition;
        _scopes = ParseScopes(apiOptions.Value.Scopes);
    }

    public async Task<string> GetAccessTokenAsync()
    {
        AuthenticationState authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        ClaimsPrincipal user = authenticationState.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            throw new SmartTaskManagerApiAccessTokenException(
                "The current web session is not authenticated. Sign in with Microsoft Entra ID and try again.");
        }

        try
        {
            return await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes, user: user);
        }
        catch (Exception exception)
        {
            throw new SmartTaskManagerApiAccessTokenException(
                "The web app could not acquire an access token for SmartTaskManager.Api. Sign out and sign in again.",
                exception);
        }
    }

    private static string[] ParseScopes(string scopes)
    {
        string[] values = scopes
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Length == 0)
        {
            throw new InvalidOperationException("SmartTaskManagerApi:Scopes must contain at least one scope.");
        }

        return values;
    }
}
