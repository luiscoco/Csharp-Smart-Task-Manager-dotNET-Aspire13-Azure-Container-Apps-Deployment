using System;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartTaskManager.Infrastructure.Persistence;

public sealed class SmartTaskManagerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SmartTaskManagerDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "SMARTTASKMANAGER_CONNECTION_STRING";
    private const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    public SmartTaskManagerDbContext CreateDbContext(string[] args)
    {
        string? connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DefaultConnectionString;
        }

        SmartTaskManagerDbContextFactory factory = new(connectionString);
        return factory.CreateDbContext();
    }
}
