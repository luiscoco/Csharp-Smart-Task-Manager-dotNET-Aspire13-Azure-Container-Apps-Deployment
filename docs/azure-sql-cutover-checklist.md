# Azure SQL Cutover Checklist

## Purpose

This checklist is the future offline cutover sequence for moving the local `SmartTaskManagerDb` SQL Server database to Azure SQL Database.

Do not execute it until the Azure SQL target, migration window, and rollback plan are approved.

## 1. Verify Source Database

- Confirm the API still uses `ConnectionStrings:SmartTaskManager`.
- Confirm the source database is still `SmartTaskManagerDb`.
- Confirm the intended source connection string is still:

```text
Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True
```

- Check for environment variables, user secrets, launch settings, or hosting overrides that point the API at a different database.
- Confirm the source database is reachable from this laptop.
- Confirm the source database contains expected tables:
  - `Users`
  - `Tasks`
  - `TaskHistoryEntries`
  - `__EFMigrationsHistory`
- Record source row counts for critical tables.
- Record the source database size.
- Stop if the database is too large for the chosen SKU or if unexpected database objects need assessment.

## 2. Provision Target

- Create one resource group.
- Create one Azure SQL logical server.
- Add a firewall rule for the current client public IP.
- Add `0.0.0.0` Azure services access only if the future Azure-hosted API needs it.
- Create one single Azure SQL Database:
  - preferred: Azure SQL Database free offer, General Purpose serverless, free limit enabled, `AutoPause`
  - paid fallback: Basic, only if source database is under `2 GB`
  - next fallback: S0 if source is over `2 GB` or Basic import is too slow
- Confirm the target database is empty before import.
- Do not start the API against the target before import.

## 3. Freeze Writes

- Stop the local API.
- Stop the local web app.
- Close admin tools, scripts, or background jobs that can change users or tasks.
- Announce the offline migration window if anyone else can use the app.
- Keep writes frozen until export, import, and validation are complete.

## 4. Export Local Database

- Use `SqlPackage /Action:Export`.
- Write the BACPAC to:

```text
C:\tmp\SmartTaskManagerDb.bacpac
```

- Keep `VerifyExtraction=True`.
- Stop if `SqlPackage` reports unsupported objects, consistency errors, or extraction failures.

## 5. Import Into Azure SQL Database

- Confirm the Azure SQL Database is still empty.
- Use `SqlPackage /Action:Import`.
- Import into the Azure SQL Database connection string with:
  - `Encrypt=True`
  - `TrustServerCertificate=False`
  - `MultipleActiveResultSets=False`
- Do not allow app startup migrations to run during the import window.

## 6. Validate Migration

- Confirm target tables exist:
  - `Users`
  - `Tasks`
  - `TaskHistoryEntries`
  - `__EFMigrationsHistory`
- Compare source and target row counts for:
  - `Users`
  - `Tasks`
  - `TaskHistoryEntries`
- Compare EF migration history rows.
- Spot-check representative records:
  - one user
  - several tasks across statuses and priorities
  - one task history chain
- Confirm foreign-key relationships are intact.
- Confirm the Azure SQL service objective is the intended free, Basic, or fallback SKU.

## 7. Switch API Configuration

- Build the Azure SQL connection string in this format:

```text
Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

- Set it as runtime configuration, not in source control:

```text
ConnectionStrings__SmartTaskManager
```

- Ensure production sample seeding remains disabled:

```text
Seeding__EnableSampleData=false
```

## 8. Smoke Test API

- Start or deploy the API with the Azure SQL connection string.
- Confirm startup succeeds.
- Confirm EF startup migration does not attempt to recreate imported schema.
- Confirm API operations work:
  - list users
  - create a low-risk test user if appropriate
  - list tasks
  - load task history
  - create, update, complete, and archive a low-risk test task if appropriate
- Review API logs for SQL connectivity or migration errors.

## 9. Smoke Test Web App

- Start or deploy the web app pointing at the API.
- Sign in through Microsoft Entra ID.
- Load dashboard, users, tasks, and task details pages.
- Confirm writes flow through the API to Azure SQL.
- Confirm no web project setting directly references SQL Server.

## 10. Rollback

If validation or smoke tests fail:

- stop the API and web app from using Azure SQL
- switch the API connection string back to the previous local or known-good database
- keep the original local `SmartTaskManagerDb` unchanged
- keep the failed BACPAC and import logs for diagnosis
- delete and recreate the Azure SQL Database if a fresh empty target is needed
- rerun export/import only after the root cause is fixed

Rollback is straightforward only if the local database remains unchanged and writes stay frozen until cutover is accepted.

## 11. Post-Cutover

- Remove any temporary firewall rule that is no longer needed.
- Decide whether `AllowAzureServices` should remain enabled or be replaced with stricter networking.
- Store SQL secrets only in the approved runtime secret store or app configuration.
- Delete local BACPAC files only after validation and backup requirements are satisfied.
- Monitor Azure SQL free-offer consumption or Basic storage usage.
- Consider replacing SQL authentication with Microsoft Entra authentication or managed identity later.

## Completed Cutover Snapshot

Execution date: `2026-05-04`

Completed Azure-side steps:

- created resource group `rg-smarttaskmanager-data-dev-weu`
- created Azure SQL logical server `sql-stm-dev-weu-01`
- created firewall rule `AllowCurrentClientIp` for `81.33.174.79`
- created firewall rule `AllowAzureServices` for `0.0.0.0`
- attempted the Azure SQL free offer first
- used Basic paid fallback after Azure returned free-offer `InternalServerError`
- created Azure SQL Database `SmartTaskManagerDb`
- confirmed the target database had `0` user-defined objects before import
- imported `C:\tmp\SmartTaskManagerDb.bacpac` successfully with `SqlPackage`

Final Azure SQL target:

- server: `sql-stm-dev-weu-01.database.windows.net`
- database: `SmartTaskManagerDb`
- edition: `Basic`
- service objective: `Basic`
- free offer used: no

Validation results:

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
- representative users:
  - `Alice`
  - `Bob`
  - `Carla`
- representative tasks:
  - `Archive old travel plan`
  - `Renew gym membership`
  - `Finish API tutorial`
  - `Prepare sprint review`
  - `Complete LINQ exercises`
- task rows with missing user reference: `0`

API app setting key for later:

```text
ConnectionStrings__SmartTaskManager
```

Final redacted API connection string:

```text
Data Source=tcp:sql-stm-dev-weu-01.database.windows.net,1433;Initial Catalog=SmartTaskManagerDb;Persist Security Info=False;User ID=stmsqladmin;Password=<redacted>;MultipleActiveResultSets=False;Connect Timeout=30;Encrypt=True;TrustServerCertificate=False
```

Remaining manual steps:

- Store or reset the SQL admin password in the approved secret store before configuring the API.
- Set `ConnectionStrings__SmartTaskManager` on the API runtime when ready for application cutover.
- Keep `Seeding__EnableSampleData=false` for production-like runtime settings.
- Decide whether to keep `AllowAzureServices` or replace it with stricter App Service outbound IP rules, private networking, or managed identity later.

## Final Reminders

- The primary data migration path is BACPAC export/import.
- EF Core startup migration is useful for normal schema evolution, but it is not a data migration mechanism.
- The target Azure SQL Database must be new or empty before import.
- Do not commit secrets or BACPAC files.
