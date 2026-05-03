using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartTaskManager.Api.Contracts.Responses;
using SmartTaskManager.Api.Data;
using SmartTaskManager.Api.Security;
using SmartTaskManager.Application.Filters;
using SmartTaskManager.Application.Services;
using Microsoft.OpenApi;

namespace SmartTaskManager.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartTaskManagerApiPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                ApiErrorResponse response = CreateValidationErrorResponse(context);
                return new BadRequestObjectResult(response);
            };
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SmartTaskManager API",
                Version = "v1",
                Summary = "Task management API built with ASP.NET Core and Clean Architecture.",
                Description =
                    "Manage users, create and update tasks, review task history, apply filters, and inspect dashboard summaries."
            });

            string xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

            if (File.Exists(xmlFilePath))
            {
                options.IncludeXmlComments(xmlFilePath);
            }

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter your Microsoft Entra access token. Example: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", null, null),
                    new List<string>()
                }
            });
        });

        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        return services;
    }

    public static IServiceCollection AddSmartTaskManagerApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        MicrosoftEntraApiOptions microsoftEntraOptions = CreateMicrosoftEntraApiOptions(configuration);

        services.AddSingleton(microsoftEntraOptions);
        services.AddSingleton<IAuthorizationHandler, DevelopmentOnlyAuthorizationHandler>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = microsoftEntraOptions.Authority;
                options.Audience = microsoftEntraOptions.EffectiveAudience;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = microsoftEntraOptions.ValidIssuers,
                    ValidateAudience = true,
                    ValidAudiences = microsoftEntraOptions.ValidAudiences,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        ILogger logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("SmartTaskManager.JwtBearer");

                        logger.LogError(
                            context.Exception,
                            "JWT authentication failed: {Message}",
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        ILogger logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("SmartTaskManager.JwtBearer");

                        string? issuer = context.Principal?.FindFirst("iss")?.Value;
                        string? audience = context.Principal?.FindFirst("aud")?.Value;
                        logger.LogInformation(
                            "JWT validated. Issuer={Issuer}, Audience={Audience}",
                            issuer, audience);

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();

                        ApiErrorResponse response = ApiErrorResponse.Create(
                            StatusCodes.Status401Unauthorized,
                            "Authentication required.",
                            "A valid bearer token is required to access this resource.",
                            context.HttpContext.TraceIdentifier,
                            context.HttpContext.Request.Path.Value ?? "/");

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return context.Response.WriteAsJsonAsync(response);
                    },
                    OnForbidden = context =>
                    {
                        ApiErrorResponse response = ApiErrorResponse.Create(
                            StatusCodes.Status403Forbidden,
                            "Access denied.",
                            "You do not have permission to perform this action.",
                            context.HttpContext.TraceIdentifier,
                            context.HttpContext.Request.Path.Value ?? "/");

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return context.Response.WriteAsJsonAsync(response);
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.DevelopmentOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new DevelopmentOnlyRequirement());
            });

            options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("roles", "Admin");
            });

            options.AddPolicy(AuthorizationPolicies.RequireEditorRole, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("roles", "Admin", "Editor");
            });
        });

        return services;
    }

    public static IServiceCollection AddSmartTaskManagerUseCaseServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<HighPriorityTaskFilter>();
        services.AddSingleton<StatusTaskFilter>();
        services.AddSingleton<OverdueTaskFilter>();

        services.AddScoped<UserService>();
        services.AddScoped<TaskService>();

        return services;
    }

    public static IServiceCollection AddSmartTaskManagerApiRuntimeServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<SampleDataSeeder>();

        return services;
    }

    private static ApiErrorResponse CreateValidationErrorResponse(ActionContext context)
    {
        Dictionary<string, string[]> errors = context.ModelState
            .Where(entry => entry.Value is not null && entry.Value.Errors.Count > 0)
            .ToDictionary(
                entry => NormalizeModelStateKey(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The input value is invalid."
                        : error.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        string path = context.HttpContext.Request.Path.HasValue
            ? context.HttpContext.Request.Path.Value!
            : "/";

        return ApiErrorResponse.Validation(
            context.HttpContext.TraceIdentifier,
            path,
            errors);
    }

    private static string NormalizeModelStateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "request";
        }

        return key;
    }

    private static MicrosoftEntraApiOptions CreateMicrosoftEntraApiOptions(IConfiguration configuration)
    {
        MicrosoftEntraApiOptions microsoftEntraOptions = configuration
            .GetSection(MicrosoftEntraApiOptions.SectionName)
            .Get<MicrosoftEntraApiOptions>() ?? new MicrosoftEntraApiOptions();

        if (string.IsNullOrWhiteSpace(microsoftEntraOptions.Instance))
        {
            throw new InvalidOperationException("AzureAd:Instance is not configured.");
        }

        if (!Uri.TryCreate(microsoftEntraOptions.Instance, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AzureAd:Instance must be a valid absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(microsoftEntraOptions.TenantId))
        {
            throw new InvalidOperationException("AzureAd:TenantId is not configured.");
        }

        if (string.IsNullOrWhiteSpace(microsoftEntraOptions.ClientId))
        {
            throw new InvalidOperationException("AzureAd:ClientId is not configured.");
        }

        if (!string.IsNullOrWhiteSpace(microsoftEntraOptions.Audience)
            && !Uri.TryCreate(microsoftEntraOptions.Audience, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("AzureAd:Audience must be a valid absolute URI when configured.");
        }

        return microsoftEntraOptions;
    }
}
