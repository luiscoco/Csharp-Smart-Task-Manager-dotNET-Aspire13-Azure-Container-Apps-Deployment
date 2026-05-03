using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace SmartTaskManager.Web.Configuration;

public static class AuthenticationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/authentication/login", (HttpContext httpContext, string? returnUrl) =>
        {
            string safeReturnUrl = GetSafeReturnUrl(returnUrl);

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                return Results.LocalRedirect(safeReturnUrl);
            }

            AuthenticationProperties properties = new()
            {
                RedirectUri = safeReturnUrl
            };

            return Results.Challenge(
                properties,
                [OpenIdConnectDefaults.AuthenticationScheme]);
        })
        .AllowAnonymous();

        endpoints.MapPost("/authentication/logout", async (HttpContext httpContext, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(httpContext);

            AuthenticationProperties properties = new()
            {
                RedirectUri = "/"
            };

            return Results.SignOut(
                properties,
                [
                    OpenIdConnectDefaults.AuthenticationScheme,
                    CookieAuthenticationDefaults.AuthenticationScheme
                ]);
        })
        .RequireAuthorization();

        return endpoints;
    }

    private static string GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return returnUrl.StartsWith("/", StringComparison.Ordinal)
            && Uri.TryCreate(returnUrl, UriKind.Relative, out _)
                ? returnUrl
                : "/";
    }
}
