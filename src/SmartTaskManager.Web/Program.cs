using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using SmartTaskManager.Web.Components;
using SmartTaskManager.Web.Configuration;
using SmartTaskManager.Web.Options;
using SmartTaskManager.Web.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

string[] downstreamApiScopes = ParseScopes(
    builder.Configuration[$"{SmartTaskManagerApiOptions.SectionName}:{nameof(SmartTaskManagerApiOptions.Scopes)}"]);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddOptions<AzureAdOptions>()
    .Bind(builder.Configuration.GetSection(AzureAdOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => !string.Equals(
            options.ClientSecret,
            AzureAdOptions.ClientSecretPlaceholder,
            StringComparison.Ordinal),
        "AzureAd:ClientSecret must be provided through user secrets or environment variables.")
    .Validate(options => IsRelativePath(options.CallbackPath), "AzureAd:CallbackPath must start with '/'.")
    .Validate(
        options => IsRelativePath(options.SignedOutCallbackPath),
        "AzureAd:SignedOutCallbackPath must start with '/'.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SmartTaskManagerApiOptions>()
    .Bind(builder.Configuration.GetSection(SmartTaskManagerApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection(AzureAdOptions.SectionName))
    .EnableTokenAcquisitionToCallDownstreamApi(downstreamApiScopes)
    .AddInMemoryTokenCaches();

builder.Services.Configure<CookieAuthenticationOptions>(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.Cookie.Name = "__Host-SmartTaskManager.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy => policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.RequireEditorRole, policy => policy.RequireRole("Admin", "Editor"));
});
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHttpClient<UsersApiClient>(ConfigureApiHttpClient);
builder.Services.AddHttpClient<TasksApiClient>(ConfigureApiHttpClient);

builder.Services.AddScoped<SmartTaskManagerApiAccessTokenProvider>();
builder.Services.AddScoped<UserSession>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapStaticAssets();
app.MapAuthenticationEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string EnsureTrailingSlash(string baseUrl)
{
    return baseUrl.EndsWith("/", StringComparison.Ordinal)
        ? baseUrl
        : $"{baseUrl}/";
}

static bool IsRelativePath(string path)
{
    return !string.IsNullOrWhiteSpace(path)
        && path.StartsWith("/", StringComparison.Ordinal);
}

static string[] ParseScopes(string? scopes)
{
    return scopes?
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? [];
}

static void ConfigureApiHttpClient(IServiceProvider serviceProvider, HttpClient httpClient)
{
    SmartTaskManagerApiOptions options = serviceProvider
        .GetRequiredService<IOptions<SmartTaskManagerApiOptions>>()
        .Value;

    httpClient.BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl), UriKind.Absolute);
    httpClient.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
}
