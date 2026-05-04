# Azure SQL Provision And Migrate Commands

## Purpose

This document provides commands to run later, after approval, for:

- provisioning one resource group
- provisioning one Azure SQL logical server
- provisioning one Azure SQL Database
- configuring firewall rules
- exporting the local SQL Server database to a `.bacpac`
- importing the `.bacpac` into Azure SQL Database
- validating schema, data, and EF migration history
- updating API runtime configuration later

Do not run these commands during preparation. They are intentionally documented as future execution steps.

## Repository Snapshot

Validated source facts:

- API project: `src/SmartTaskManager.Api`
- API connection string name: `ConnectionStrings:SmartTaskManager`
- runtime source connection string from `src/SmartTaskManager.Api/appsettings.json`:

```text
Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True
```

- API startup applies EF migrations through `DatabaseInitializer.ApplyMigrationsAsync()`
- Web app calls the API through HTTP clients and `SmartTaskManagerApi:BaseUrl`; it does not directly connect to SQL Server
- repository-visible schema is ordinary EF Core SQL Server schema with `Users`, `Tasks`, and `TaskHistoryEntries`

Important migration rule:

- use BACPAC export/import as the primary data migration path
- do not rely on API startup EF migrations to move existing local data

## Prerequisites

Install or verify these tools before execution:

```powershell
az version
sqlpackage /Version
sqlcmd -?
dotnet --info
```

Install or update `SqlPackage` if needed:

```powershell
dotnet tool install -g microsoft.sqlpackage
dotnet tool update -g microsoft.sqlpackage
```

Sign in to Azure:

```powershell
az login
az account show
```

Select the intended subscription:

```powershell
$SubscriptionId = "<subscription-id>"
az account set --subscription $SubscriptionId
```

Check Azure CLI support for the free-offer flags:

```powershell
az sql db create --help | Select-String -Pattern "use-free-limit|free-limit-exhaustion"
```

Check available SQL Database editions in the target region:

```powershell
$Location = "westeurope"
az sql db list-editions -l $Location -o table
```

Free-offer eligibility still must be confirmed at provisioning time. If the free-offer command fails or the Azure portal does not show the free offer for the selected subscription and region, use the Basic paid fallback command.

## Working Variables

Use globally unique logical server names. The values below are examples.

```powershell
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-data-dev"
$SqlServerName = "sqlstmdev001"
$DatabaseName = "SmartTaskManagerDb"
$SqlAdminUser = "sqladminstm"
$SqlAdminPassword = "<strong-password-not-in-source-control>"

$PublicIp = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
$BacpacPath = "C:\tmp\SmartTaskManagerDb.bacpac"

$SourceConnectionString = "Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
$TargetConnectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$SqlAdminUser;Password=$SqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

Do not commit real passwords, final connection strings, or generated BACPAC files.

## Actual Execution Snapshot

Execution date: `2026-05-04`

Actual values used:

```powershell
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-data-dev-weu"
$SqlServerName = "sql-stm-dev-weu-01"
$DatabaseName = "SmartTaskManagerDb"
$SqlAdminUser = "stmsqladmin"
$BacpacPath = "C:\tmp\SmartTaskManagerDb.bacpac"
```

Actual Azure account context:

- subscription: `Subscription 2`
- subscription ID: `e5bd93f3-dcd9-4833-a589-82e16245997c`
- tenant ID: `e099cebd-5eea-41a3-88db-bcb9a9cba83e`

Actual source artifact:

- source instance confirmed by SSMS screenshot: `(localdb)\MSSQLLocalDB`
- source database: `SmartTaskManagerDb`
- export method: SSMS `Export Data-tier Application`
- BACPAC file: `C:\tmp\SmartTaskManagerDb.bacpac`
- BACPAC size before import: `9,823` bytes
- BACPAC modified time before import: `2026-05-04 09:36:23`

Actual provisioning result:

- resource group created: `rg-smarttaskmanager-data-dev-weu`
- Azure SQL logical server created: `sql-stm-dev-weu-01`
- Azure SQL logical server FQDN: `sql-stm-dev-weu-01.database.windows.net`
- firewall rule `AllowCurrentClientIp`: `81.33.174.79` to `81.33.174.79`
- firewall rule `AllowAzureServices`: `0.0.0.0` to `0.0.0.0`
- target database created: `SmartTaskManagerDb`

Free-offer attempt result:

- the free-offer command was attempted first
- Azure returned `InternalServerError` twice before any database was created
- tracking IDs:
  - `6c46f107-f9d2-4add-ba5f-16a5dd322961`
  - `d1b67085-95fd-4bc0-9d57-b35cab0faf91`

Actual SKU after fallback:

- edition: `Basic`
- service objective: `Basic`
- SKU: `Basic`
- status after validation: `Online`
- `useFreeLimit`: `null`
- `freeLimitExhaustionBehavior`: `null`

Import result:

- target user-defined object count before import: `0`
- import tool: `SqlPackage` version `170.2.70.1`
- import source file: `C:\tmp\SmartTaskManagerDb.bacpac`
- import result: successful
- import elapsed time: `0:07:36.17`

Validation result:

- tables present:
  - `__EFMigrationsHistory`
  - `TaskHistoryEntries`
  - `Tasks`
  - `Users`
- row counts:
  - `Users`: `3`
  - `Tasks`: `9`
  - `TaskHistoryEntries`: `13`
- EF migration history:
  - `20260408141007_InitialSqlServer` / `10.0.0`
- target service objective: `Basic`
- task rows with missing user reference: `0`
- spot-checked users included `Alice`, `Bob`, and `Carla`
- spot-checked task titles included `Archive old travel plan`, `Renew gym membership`, `Finish API tutorial`, `Prepare sprint review`, and `Complete LINQ exercises`

Final API setting key:

```text
ConnectionStrings__SmartTaskManager
```

Final redacted API connection string:

```text
Data Source=tcp:sql-stm-dev-weu-01.database.windows.net,1433;Initial Catalog=SmartTaskManagerDb;Persist Security Info=False;User ID=stmsqladmin;Password=<redacted>;MultipleActiveResultSets=False;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False
```

The SQL admin password was generated and reset during execution. It was not written to tracked files. If the cleartext password is needed later and was not captured in the approved secret store, reset the SQL admin password before setting the API runtime connection string.

## Preflight Source Database Checks

Run these before export to confirm the actual local database matches the repository assumptions.

Confirm the database is reachable:

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName;"
```

Check source size. Basic paid fallback is only suitable when the actual source fits under `2 GB`.

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT DB_NAME() AS DatabaseName, CAST(SUM(size) * 8.0 / 1024 AS decimal(18,2)) AS SizeMB FROM sys.database_files;"
```

Check user-defined object inventory:

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT type_desc, COUNT(*) AS ObjectCount FROM sys.objects WHERE is_ms_shipped = 0 GROUP BY type_desc ORDER BY type_desc;"
```

Expected repository-aligned objects are user tables, keys, indexes, and constraints for the EF schema. If this query reveals extra object types that are not in the repository, assess them before export.

Check EF migration history:

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;"
```

Check source row counts:

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users UNION ALL SELECT 'Tasks', COUNT(*) FROM Tasks UNION ALL SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries;"
```

If the database is large, contains unexpected objects, or contains unsupported features, stop and use the fallback path documented near the end of this file.

## Provisioning Azure SQL Logical Server

Create the resource group:

```powershell
az group create `
  --name $ResourceGroup `
  --location $Location
```

Create the Azure SQL logical server:

```powershell
az sql server create `
  --resource-group $ResourceGroup `
  --name $SqlServerName `
  --location $Location `
  --admin-user $SqlAdminUser `
  --admin-password $SqlAdminPassword
```

## Firewall Rules

Allow the current client public IP:

```powershell
az sql server firewall-rule create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name "AllowCurrentClientIp" `
  --start-ip-address $PublicIp `
  --end-ip-address $PublicIp
```

Allow Azure services only if the future App Service or Azure-hosted API needs this broad connectivity path:

```powershell
az sql server firewall-rule create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name "AllowAzureServices" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

Security note: `0.0.0.0` allows connections from Azure services beyond this subscription. Keep SQL credentials and permissions tight, and prefer private endpoints or more restrictive networking for production.

## Provisioning Azure SQL Database

### Preferred: Free Offer

Use first if supported by the subscription and region:

```powershell
az sql db create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --edition GeneralPurpose `
  --family Gen5 `
  --capacity 1 `
  --compute-model Serverless `
  --min-capacity 0.5 `
  --auto-pause-delay 60 `
  --backup-storage-redundancy Local `
  --use-free-limit `
  --free-limit-exhaustion-behavior AutoPause
```

Create this database first, keep it empty, and then import with `SqlPackage`. If Azure rejects BACPAC import into a free-offer database in the selected subscription, use the Basic paid fallback.

Verify the created shape:

```powershell
az sql db show `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --query "{name:name,status:status,edition:edition,currentServiceObjective:currentServiceObjectiveName,sku:sku.name,useFreeLimit:useFreeLimit,freeLimitExhaustionBehavior:freeLimitExhaustionBehavior}" `
  -o table
```

### Paid Fallback: Basic

Use if the free offer is unavailable and the source database is under `2 GB`:

```powershell
az sql db create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --edition Basic `
  --service-objective Basic `
  --max-size 2GB `
  --backup-storage-redundancy Local
```

If source size is over `2 GB`, use `S0` as the next low-cost fallback:

```powershell
az sql db create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --edition Standard `
  --service-objective S0 `
  --max-size 250GB `
  --backup-storage-redundancy Local
```

### Confirm Empty Target Before Import

`SqlPackage` import can target a new or existing empty database. It should not target a database with user-defined schema objects.

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT COUNT(*) AS UserObjectCount FROM sys.objects WHERE is_ms_shipped = 0;"
```

Expected before import:

```text
UserObjectCount
---------------
0
```

Do not start the API against this database before import. The API startup migration could create schema objects and make the target non-empty.

## Local Database Export To `.bacpac`

Freeze writes first:

- stop local API processes
- stop local Web processes
- close tools or scripts that can write to `SmartTaskManagerDb`
- keep the source unchanged until cutover validation completes

Create the local export folder:

```powershell
New-Item -ItemType Directory -Path "C:\tmp" -Force
```

Export:

```powershell
sqlpackage /Action:Export `
  /SourceConnectionString:"$SourceConnectionString" `
  /TargetFile:"$BacpacPath" `
  /p:CommandTimeout=1200 `
  /p:VerifyExtraction=True
```

Confirm the file exists:

```powershell
Get-Item $BacpacPath
```

## Import To Azure SQL Database

Import into the already-created empty Azure SQL Database:

```powershell
sqlpackage /Action:Import `
  /SourceFile:"$BacpacPath" `
  /TargetConnectionString:"$TargetConnectionString" `
  /p:CommandTimeout=1200
```

Do not add `DatabaseEdition` or `DatabaseServiceObjective` properties when importing into a pre-created database. The provisioned database SKU controls the target shape.

If import is too slow on Basic, scale up temporarily, import, validate, then scale back down:

```powershell
az sql db update `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --edition Standard `
  --service-objective S0
```

Scale back to Basic after validation if the database is still under `2 GB`:

```powershell
az sql db update `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --edition Basic `
  --service-objective Basic `
  --max-size 2GB
```

## Validation Queries And Checks

Check target tables:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT name FROM sys.tables ORDER BY name;"
```

Check EF migration history:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;"
```

Compare source row counts:

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users UNION ALL SELECT 'Tasks', COUNT(*) FROM Tasks UNION ALL SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries;"
```

Compare target row counts:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users UNION ALL SELECT 'Tasks', COUNT(*) FROM Tasks UNION ALL SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries;"
```

Check target service objective:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT edition, service_objective FROM sys.database_service_objectives WHERE database_id = DB_ID();"
```

Spot-check representative data:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT TOP (5) Id, UserName, CreatedOnUtc FROM Users ORDER BY CreatedOnUtc;"
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT TOP (5) Id, Title, UserId, DueDate, Priority, Status FROM Tasks ORDER BY DueDate;"
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT TOP (5) Id, TaskId, Sequence, Action, OccurredOnUtc FROM TaskHistoryEntries ORDER BY OccurredOnUtc;"
```

## Updating The API App Setting Later

When cutover is approved, set the API runtime setting. Do not write the final connection string into tracked appsettings files.

For App Service app settings:

```powershell
$ApiResourceGroup = "<api-app-resource-group>"
$ApiAppName = "<api-app-service-name>"

az webapp config appsettings set `
  --resource-group $ApiResourceGroup `
  --name $ApiAppName `
  --settings `
    ConnectionStrings__SmartTaskManager="$TargetConnectionString" `
    Seeding__EnableSampleData="false"
```

Expected target connection string format:

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Cleanup Commands

Delete only the database:

```powershell
az sql db delete `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --yes
```

Delete the Azure SQL logical server:

```powershell
az sql server delete `
  --resource-group $ResourceGroup `
  --name $SqlServerName `
  --yes
```

Delete the whole resource group:

```powershell
az group delete `
  --name $ResourceGroup `
  --yes `
  --no-wait
```

Delete the local BACPAC after the migration has been validated and the backup is no longer needed:

```powershell
Remove-Item -LiteralPath $BacpacPath -Force
```

## Optional Prepared Scripts

The scripts in `scripts/` are dry-run by default and only execute when `-Execute` is provided.

Dry-run provisioning:

```powershell
.\scripts\provision-azure-sql.ps1 `
  -ResourceGroup $ResourceGroup `
  -Location $Location `
  -SqlServerName $SqlServerName `
  -DatabaseName $DatabaseName `
  -SqlAdminUser $SqlAdminUser `
  -Sku Free
```

Future execution after approval:

```powershell
.\scripts\provision-azure-sql.ps1 `
  -ResourceGroup $ResourceGroup `
  -Location $Location `
  -SqlServerName $SqlServerName `
  -DatabaseName $DatabaseName `
  -SqlAdminUser $SqlAdminUser `
  -SqlAdminPassword (Read-Host "SQL admin password" -AsSecureString) `
  -Sku Free `
  -AllowAzureServices `
  -Execute
```

Dry-run migration:

```powershell
.\scripts\migrate-local-sql-to-azure-sql.ps1 `
  -TargetServerName $SqlServerName `
  -TargetDatabaseName $DatabaseName `
  -TargetUser $SqlAdminUser
```

Future execution after approval:

```powershell
.\scripts\migrate-local-sql-to-azure-sql.ps1 `
  -TargetServerName $SqlServerName `
  -TargetDatabaseName $DatabaseName `
  -TargetUser $SqlAdminUser `
  -TargetPassword (Read-Host "SQL admin password" -AsSecureString) `
  -Execute
```

Future validation after import:

```powershell
.\scripts\validate-azure-sql-migration.ps1 `
  -TargetConnectionString $TargetConnectionString `
  -Execute
```

## Fallback Path

Use this path only if preflight or migration detects a real blocker:

- source database is too large for practical BACPAC movement
- source database is larger than the selected paid fallback storage limit
- `SqlPackage` reports unsupported schema objects
- export or import repeatedly times out
- object inventory reveals non-EF database objects that need assessment

Fallback options:

- current Visual Studio Code/MSSQL database tooling for assessment
- Azure Data Studio plus the Azure SQL migration extension if that toolchain is still available in your environment; Microsoft documentation states Azure Data Studio retired on February 28, 2026
- Azure Database Migration Service offline migration to Azure SQL Database
- temporary scale-up of Azure SQL Database during import, followed by scale-down after validation

## Sources Consulted

- [Deploy Azure SQL Database for free](https://learn.microsoft.com/en-us/azure/azure-sql/database/free-offer?view=azuresql)
- [az sql db create](https://learn.microsoft.com/en-us/cli/azure/sql/db?view=azure-cli-lts)
- [SqlPackage Export parameters and properties](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-export?view=sql-server-ver17)
- [SqlPackage Import parameters and properties](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-import?view=sql-server-ver17)
- [Import a BACPAC file to a database in Azure SQL Database](https://learn.microsoft.com/en-us/azure/azure-sql/database/database-import?view=azuresql)
- [Azure SQL Database firewall rules](https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-configure?view=azuresql)
- [Azure Database Migration Service offline migration to Azure SQL Database](https://learn.microsoft.com/en-us/azure/dms/tutorial-sql-server-azure-sql-database-offline)
- [Azure SQL migration extension for Azure Data Studio](https://learn.microsoft.com/en-us/sql/azure-data-studio/extensions/azure-sql-migration-extension?view=sql-server-ver16)
