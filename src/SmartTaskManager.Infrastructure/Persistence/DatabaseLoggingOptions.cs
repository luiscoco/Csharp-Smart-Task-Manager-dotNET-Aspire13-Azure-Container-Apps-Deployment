namespace SmartTaskManager.Infrastructure.Persistence;

public sealed class DatabaseLoggingOptions
{
    public bool EnableEfLogging { get; init; }

    public bool EnableDetailedErrors { get; init; }

    public bool EnableSensitiveDataLogging { get; init; }
}
