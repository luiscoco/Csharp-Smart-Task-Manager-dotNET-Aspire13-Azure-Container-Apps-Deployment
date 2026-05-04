# Azure SQL Options

## Scope

This document prepares the Azure SQL target for SmartTaskManager without deploying resources or moving data.

It compares the Azure SQL Database free offer with the cheapest paid Azure SQL Database fallback, validates repository assumptions from source, and selects the lowest-cost target that fits this app and an offline BACPAC migration.

## Repository Facts Verified From Source

| Fact | Verified result | Source |
| --- | --- | --- |
| API project | `src/SmartTaskManager.Api` | `SmartTaskManager.sln`, `src/SmartTaskManager.Api/SmartTaskManager.Api.csproj` |
| API connection string name | `ConnectionStrings:SmartTaskManager` | `src/SmartTaskManager.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` |
| Runtime local default database | `SmartTaskManagerDb` on `localhost` | `src/SmartTaskManager.Api/appsettings.json` |
| Runtime local default connection string | `Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True` | `src/SmartTaskManager.Api/appsettings.json` |
| EF provider | SQL Server provider with retry-on-failure enabled | `src/SmartTaskManager.Infrastructure/Persistence/SmartTaskManagerDbContextFactory.cs` |
| API startup migrations | API calls `InitializeSmartTaskManagerAsync()`, which calls `DatabaseInitializer.ApplyMigrationsAsync()`, which calls `MigrateAsync()` | `src/SmartTaskManager.Api/Program.cs`, `src/SmartTaskManager.Api/Configuration/WebApplicationExtensions.cs`, `src/SmartTaskManager.Infrastructure/Persistence/DatabaseInitializer.cs` |
| Web app SQL usage | No direct SQL connection; it configures typed HTTP clients for the API using `SmartTaskManagerApi:BaseUrl` | `src/SmartTaskManager.Web/Program.cs`, `src/SmartTaskManager.Web/appsettings.json` |

Design-time EF tooling has a different fallback connection string using `(localdb)\MSSQLLocalDB` in `SmartTaskManagerDesignTimeDbContextFactory.cs`. That does not change the API runtime default, which is the `localhost` connection string above.

## Migration Fit Assessment

The repository-visible schema is a good fit for Azure SQL Database and for `SqlPackage` export/import.

Detected EF-managed tables:

- `Users`
- `Tasks`
- `TaskHistoryEntries`
- `__EFMigrationsHistory` when the database has been created by EF migrations

Detected SQL types and objects are conventional Azure SQL-compatible SQL Server objects:

- `uniqueidentifier`
- `nvarchar`
- `datetime2`
- `int`
- primary keys
- foreign keys
- simple indexes

No repository-visible Azure SQL compatibility blockers were found in the app source or current EF migration.

The actual local database should still be checked before export because it could contain objects created outside EF Core. The command document includes preflight queries for database size, object inventory, table row counts, and EF migration history.

## BACPAC Strategy Fit

Primary migration strategy:

- offline `SqlPackage /Action:Export`
- local `.bacpac` file under `C:\tmp`
- offline `SqlPackage /Action:Import`
- application write freeze during export and import

Why this is appropriate:

- the source schema is small and ordinary
- the app uses EF Core against SQL Server/Azure SQL-compatible objects
- a BACPAC includes schema and table data, so it preserves the EF schema and the `__EFMigrationsHistory` table when that table exists in the source database
- the Azure target can be created first as an empty Azure SQL Database, then imported into

Important constraints:

- the target database must be new or empty before import; it cannot contain user-defined schema objects
- API startup migrations must not be used as the primary data migration path because they create/update schema but do not copy existing local data
- for transactional consistency, writes must be stopped during export or the export must be taken from a transactionally consistent copy
- `SqlPackage` performs best for databases under 200 GB; this app is expected to be far smaller, but actual local size must be checked before migration

Fallback path if preflight or export/import exposes a blocker:

- use current Visual Studio Code/MSSQL database tooling for assessment, or Azure Data Studio plus the Azure SQL migration extension only if that toolchain is still available in your environment; Microsoft documentation states Azure Data Studio retired on February 28, 2026
- use Azure Database Migration Service offline migration for a more guided offline move
- use a larger temporary target SKU for import, then scale down after validation

Do not use the fallback unless a real blocker appears. Nothing in the repository currently justifies that extra complexity.

## Option 1: Azure SQL Database Free Offer

Selected first choice if the subscription and target region support it.

Exact target shape:

- resource type: Azure SQL Database single database
- server type: Azure SQL logical server
- purchasing model: vCore
- service tier: General Purpose
- compute tier: Serverless
- hardware family: Gen5
- max vCores: `1`
- min vCores: `0.5`
- service objective shape: `GP_S_Gen5_1`
- auto-pause delay: `60` minutes
- backup redundancy: Local
- free limit enabled
- free limit exhaustion behavior: `AutoPause`

Why this is the lowest-cost recommendation:

- it can be free for the lifetime of the subscription limits
- current Microsoft documentation lists monthly free limits of `100,000` vCore seconds, `32 GB` data storage, and `32 GB` backup storage per free offer database
- up to `10` free offer General Purpose databases are available per subscription
- the app is small, intermittently used, and has a simple schema
- local backup redundancy and auto-pause are acceptable for a dev/test or portfolio app

Tradeoffs:

- not intended as a production SLA-backed database
- when `AutoPause` is selected and the free limit is exhausted, the database can become unavailable until the next calendar month
- long-term backup retention is unavailable with the auto-pause free behavior
- local backup redundancy only with the auto-pause free behavior
- not supported in elastic pools or failover groups
- once a region is selected for a subscription's free databases, that region applies to the subscription's free database offer
- eligibility must be confirmed at provisioning time because subscription state and regional availability are not visible from repository source
- create the free-offer database first and keep it empty for `SqlPackage` import; if Azure rejects BACPAC import into a free-offer database in the selected subscription, switch to the Basic paid fallback

## Option 2: Cheapest Paid Fallback

Use this only when the Azure SQL Database free offer is unavailable or unsuitable.

Exact paid fallback selected:

- resource type: Azure SQL Database single database
- server type: Azure SQL logical server
- purchasing model: DTU
- service tier: Basic
- service objective: `Basic`
- DTUs: `5`
- max storage: `2 GB`
- backup redundancy: Local

Why this is the cheapest paid fallback:

- Azure SQL Database Basic is the smallest paid single-database tier
- it is suitable for this repository's simple schema and low expected workload
- it avoids paying for serverless vCore compute when the free offer is not available
- it supports an empty Azure SQL Database target that can receive a BACPAC import

Paid fallback constraints:

- the actual source database must fit under the Basic tier's `2 GB` storage limit
- import may be slower on Basic; if import duration or timeout becomes a problem, temporarily scale to `S0` or another larger tier for import and scale back after validation
- if the source database is over `2 GB`, use `S0` as the next low-cost paid single-database fallback because it supports much larger storage

## Selected Recommendation

Recommendation:

1. Try the Azure SQL Database free offer first using General Purpose serverless Gen5 with free limits enabled and `AutoPause`.
2. If the free offer is unavailable, use Azure SQL Database Basic paid fallback only if the source database is under `2 GB`.
3. If the source database is over `2 GB` or Basic import performance is not acceptable, use `S0` temporarily or permanently as the next low-cost paid fallback.

For SmartTaskManager, this is the cheapest reasonable design because the repository shows a small EF-managed schema with no detected Azure SQL compatibility blockers.

## Actual Provisioning Result

Execution date: `2026-05-04`

Actual Azure values used:

- subscription: `Subscription 2` (`e5bd93f3-dcd9-4833-a589-82e16245997c`)
- tenant: `e099cebd-5eea-41a3-88db-bcb9a9cba83e`
- resource group: `rg-smarttaskmanager-data-dev-weu`
- location: `westeurope`
- Azure SQL logical server: `sql-stm-dev-weu-01`
- Azure SQL logical server FQDN: `sql-stm-dev-weu-01.database.windows.net`
- database: `SmartTaskManagerDb`
- SQL admin username: `stmsqladmin`
- SQL admin password: generated during execution and not written to tracked files

Free-offer attempt result:

- The free-offer path was attempted first as planned.
- Azure returned `InternalServerError` for the free-offer create command twice.
- Tracking IDs recorded by Azure CLI:
  - `6c46f107-f9d2-4add-ba5f-16a5dd322961`
  - `d1b67085-95fd-4bc0-9d57-b35cab0faf91`

Actual SKU used after fallback:

- resource type: Azure SQL Database single database
- purchasing model: DTU
- service tier: Basic
- service objective: `Basic`
- max storage: `2 GB`
- backup redundancy: Local
- free offer used: no, because Azure rejected the free-offer create operation

The Basic fallback is still aligned with the selected recommendation because the imported database is very small and the free-offer create operation failed before any database was created.

## Future Azure SQL Connection String

When provisioned later, the API should receive this format through runtime configuration, not source control:

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Recommended API setting name for later App Service or container configuration:

```text
ConnectionStrings__SmartTaskManager
```

Do not store the SQL administrator password or final connection string in tracked files.

## Sources Consulted

- [Deploy Azure SQL Database for free](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer?view=azuresql)
- [Azure SQL Database free offer FAQ](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer-faq?view=azuresql)
- [SqlPackage Export parameters and properties](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-export?view=sql-server-ver17)
- [SqlPackage Import parameters and properties](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-import?view=sql-server-ver17)
- [Import a BACPAC file to a database in Azure SQL Database](https://learn.microsoft.com/en-us/azure/azure-sql/database/database-import?view=azuresql)
- [Azure SQL Database serverless compute tier](https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview?view=azuresql)
- [Azure SQL Database firewall rules](https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-configure?view=azuresql)
- [Azure SQL Database pricing](https://azure.microsoft.com/en-us/pricing/details/azure-sql-database/single/)
- [Azure SQL migration extension for Azure Data Studio](https://learn.microsoft.com/en-us/sql/azure-data-studio/extensions/azure-sql-migration-extension?view=sql-server-ver16)
