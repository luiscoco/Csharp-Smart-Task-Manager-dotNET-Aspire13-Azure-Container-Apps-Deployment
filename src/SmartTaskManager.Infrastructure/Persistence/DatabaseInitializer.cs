using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SmartTaskManager.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly SmartTaskManagerDbContextFactory _dbContextFactory;

    public DatabaseInitializer(SmartTaskManagerDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using SmartTaskManagerDbContext dbContext = _dbContextFactory.CreateDbContext();

        if (await HasLegacySchemaWithoutMigrationsAsync(dbContext, cancellationToken))
        {
            throw new InvalidOperationException(
                "The database was created before EF Core migrations were enabled. Drop the existing SmartTaskManagerDb database once, then run the application again.");
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<bool> HasLegacySchemaWithoutMigrationsAsync(
        SmartTaskManagerDbContext dbContext,
        CancellationToken cancellationToken)
    {
        bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return false;
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            bool hasMigrationHistory = await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);
            if (hasMigrationHistory)
            {
                return false;
            }

            bool hasUsersTable = await TableExistsAsync(connection, "Users", cancellationToken);
            bool hasTasksTable = await TableExistsAsync(connection, "Tasks", cancellationToken);
            bool hasTaskHistoryTable = await TableExistsAsync(connection, "TaskHistoryEntries", cancellationToken);

            return hasUsersTable || hasTasksTable || hasTaskHistoryTable;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(
        DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = @tableName
                )
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END
            """;

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true || result is 1;
    }
}
