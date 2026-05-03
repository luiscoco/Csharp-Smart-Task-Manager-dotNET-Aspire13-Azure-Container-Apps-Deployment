using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartTaskManager.Application.Abstractions.Notifications;
using SmartTaskManager.Application.Abstractions.Persistence;
using SmartTaskManager.Application.Abstractions.Services;
using SmartTaskManager.Domain.Entities;
using SmartTaskManager.Infrastructure.Notifications;
using SmartTaskManager.Infrastructure.Persistence;
using SmartTaskManager.Infrastructure.Repositories;
using SmartTaskManager.Infrastructure.Time;

namespace SmartTaskManager.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartTaskManagerInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<INotificationService, SilentNotificationService>();
        services.AddSingleton(CreateDbContextFactory);

        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IRepository<User>, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }

    private static SmartTaskManagerDbContextFactory CreateDbContextFactory(IServiceProvider serviceProvider)
    {
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        string connectionString = configuration.GetConnectionString("SmartTaskManager")
            ?? throw new InvalidOperationException("Connection string 'SmartTaskManager' is not configured.");

        DatabaseLoggingOptions loggingOptions = new()
        {
            EnableEfLogging = GetBoolean(configuration, "Database:EnableEfLogging"),
            EnableDetailedErrors = GetBoolean(configuration, "Database:EnableDetailedErrors"),
            EnableSensitiveDataLogging = GetBoolean(configuration, "Database:EnableSensitiveDataLogging")
        };

        ILogger logger = loggerFactory.CreateLogger("SmartTaskManager.Database");

        return new SmartTaskManagerDbContextFactory(
            connectionString,
            loggingOptions,
            message => logger.LogInformation("{EfMessage}", message));
    }

    private static bool GetBoolean(IConfiguration configuration, string key)
    {
        return bool.TryParse(configuration[key], out bool value) && value;
    }
}
