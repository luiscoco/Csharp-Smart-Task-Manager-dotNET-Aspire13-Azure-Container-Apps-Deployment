using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SmartTaskManager.Infrastructure.Persistence;

public sealed class SmartTaskManagerDbContextFactory
{
    private readonly DbContextOptions<SmartTaskManagerDbContext> _options;

    public SmartTaskManagerDbContextFactory(
        string connectionString,
        DatabaseLoggingOptions? loggingOptions = null,
        Action<string>? logAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        loggingOptions ??= new DatabaseLoggingOptions();

        DbContextOptionsBuilder<SmartTaskManagerDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlServer(
            connectionString,
            sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());

        if (loggingOptions.EnableDetailedErrors)
        {
            optionsBuilder.EnableDetailedErrors();
        }

        if (loggingOptions.EnableSensitiveDataLogging)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        if (loggingOptions.EnableEfLogging && logAction is not null)
        {
            optionsBuilder.LogTo(
                logAction,
                new[]
                {
                    DbLoggerCategory.Database.Command.Name,
                    DbLoggerCategory.Migrations.Name,
                    DbLoggerCategory.Update.Name,
                    DbLoggerCategory.Infrastructure.Name
                },
                LogLevel.Information,
                DbContextLoggerOptions.SingleLine);
        }

        _options = optionsBuilder.Options;
    }

    public SmartTaskManagerDbContext CreateDbContext()
    {
        return new SmartTaskManagerDbContext(_options);
    }
}
