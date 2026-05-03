# Azure SQL Provision And Migrate Commands

## Purpose

This document provides non-executed commands for:

- provisioning an Azure SQL logical server
- provisioning one Azure SQL Database
- exporting the local SQL Server database to a `.bacpac`
- importing the `.bacpac` into Azure SQL Database
- validating the result

The current source database assumption is:

```text
Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True
```

## Prerequisites

### Local Tools

- Azure CLI
- .NET 10 SDK
- `SqlPackage`
- PowerShell

Recommended `SqlPackage` installation:

```powershell
dotnet tool install -g microsoft.sqlpackage
```

If already installed:

```powershell
dotnet tool update -g microsoft.sqlpackage
sqlpackage /Version
```

### Azure Login

```powershell
az login
az account show
```

Optional subscription selection:

```powershell
$SubscriptionId = "<subscription-id>"
az account set --subscription $SubscriptionId
```

### Working Variables

```powershell
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-data-dev"
$SqlServerName = "sqlstmdev001"
$DatabaseName = "SmartTaskManagerDb"
$SqlAdminUser = "sqladminstm"
$SqlAdminPassword = "<strong-password>"

$PublicIp = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
$BacpacPath = "C:\tmp\SmartTaskManagerDb.bacpac"

$SourceConnectionString = "Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True"
$TargetConnectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$SqlAdminUser;Password=$SqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### Check Supported Azure SQL Editions In A Region

```powershell
az sql db list-editions -l $Location -o table
```

Use this to confirm the desired serverless SKU is supported in your region.

## Provisioning Azure SQL Logical Server

### Create The Resource Group

```powershell
az group create --name $ResourceGroup --location $Location
```

### Create The Azure SQL Logical Server

```powershell
az sql server create `
  --resource-group $ResourceGroup `
  --name $SqlServerName `
  --location $Location `
  --admin-user $SqlAdminUser `
  --admin-password $SqlAdminPassword
```

## Firewall Rules

### Allow The Current Client IP

```powershell
az sql server firewall-rule create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name "AllowCurrentClientIp" `
  --start-ip-address $PublicIp `
  --end-ip-address $PublicIp
```

### Allow Azure Services

This is the low-cost App Service-friendly option for the future web/API apps.

```powershell
az sql server firewall-rule create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name "AllowAzureServices" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

## Provisioning Azure SQL Database

## Free Offer Profile

Use this first if the subscription is eligible:

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

Notes:

- this creates the database as a free-offer serverless database when supported
- the database must remain empty before import

## Paid Fallback Profile

Use this if the free offer is unavailable:

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
  --backup-storage-redundancy Local
```

## Local Database Export To `.bacpac`

Before export:

- stop writes to the application
- ensure the database is not being modified during export

Recommended export command:

```powershell
sqlpackage /Action:Export `
  /SourceConnectionString:"$SourceConnectionString" `
  /TargetFile:"$BacpacPath" `
  /p:CommandTimeout=1200 `
  /p:VerifyExtraction=True
```

Notes:

- this is an offline migration plan
- Microsoft recommends transactional consistency during export
- for this small repository schema, a local offline export is the right first path

## Import To Azure SQL Database

Important:

- the target database must be new or empty
- do not let the API create schema objects in the target before import

Recommended import command:

```powershell
sqlpackage /Action:Import `
  /SourceFile:"$BacpacPath" `
  /TargetConnectionString:"$TargetConnectionString" `
  /p:CommandTimeout=1200 `
  /p:DatabaseEdition=GeneralPurpose `
  /p:DatabaseServiceObjective=GP_S_Gen5_1
```

If the free profile was used, keep the database empty and use the same import shape. The free-offer database is still an Azure SQL Database target.

## Validation Queries And Checks

### Check Core Tables In The Target

Use Azure SQL with:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT name FROM sys.tables ORDER BY name;"
```

### Check EF Migration History

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;"
```

### Compare Row Counts

Source:

```powershell
sqlcmd -S "localhost" -d "SmartTaskManagerDb" -E -Q "SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users UNION ALL SELECT 'Tasks', COUNT(*) FROM Tasks UNION ALL SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries;"
```

Target:

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users UNION ALL SELECT 'Tasks', COUNT(*) FROM Tasks UNION ALL SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries;"
```

### Confirm The Database Service Objective

```powershell
sqlcmd -S "$SqlServerName.database.windows.net" -d $DatabaseName -U $SqlAdminUser -P $SqlAdminPassword -Q "SELECT edition, service_objective FROM sys.database_service_objectives WHERE database_id = DB_ID();"
```

## Updating The API App Setting Later

The future API runtime setting should be:

```text
ConnectionStrings__SmartTaskManager
```

Example value:

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Cleanup Commands

Delete the database only:

```powershell
az sql db delete `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name $DatabaseName `
  --yes
```

Delete the logical server:

```powershell
az sql server delete `
  --resource-group $ResourceGroup `
  --name $SqlServerName `
  --yes
```

Delete the whole resource group:

```powershell
az group delete --name $ResourceGroup --yes --no-wait
```

Delete the local `.bacpac` file:

```powershell
Remove-Item $BacpacPath -Force
```

## Important Notes

- `SqlPackage` import should be treated as the primary migration mechanism, not EF Core startup migration.
- EF Core startup migration is still expected later for normal application startup safety, but not for copying existing local data into Azure.
- If the real local database turns out to use unsupported features that are not visible from the repository alone, switch to the documented Azure Data Studio or Azure Database Migration Service fallback.

## Execution Snapshot

Actual values used:

```powershell
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-data-dev-weu"
$SqlServerName = "sql-stm-dev-weu-01"
$DatabaseName = "SmartTaskManagerDb"
$SqlAdminUser = "stmsqladmin"
$BacpacPath = "C:\tmp\SmartTaskManagerDb.bacpac"
```

Actual source used for migration:

- source type: local `.bacpac`
- source file: `C:\tmp\SmartTaskManagerDb.bacpac`

Actual result:

- Azure SQL logical server exists
- Azure SQL Database exists and is online
- final database service objective is `GP_S_Gen5_1`
- import completed successfully

Validated target counts after import:

- `Users`: `3`
- `Tasks`: `9`
- `TaskHistoryEntries`: `13`
- `__EFMigrationsHistory`: `1`

Validated migration row:

- `20260408141007_InitialSqlServer`
