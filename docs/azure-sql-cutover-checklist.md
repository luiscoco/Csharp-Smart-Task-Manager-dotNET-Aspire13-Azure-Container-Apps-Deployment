# Azure SQL Cutover Checklist

## Purpose

This checklist is the execution sequence for moving the current local SQL Server database to Azure SQL Database with an offline cutover.

## 1. Verify Source Database

- Confirm the source instance is still the expected runtime target:
  `localhost`
- Confirm the source database is still:
  `SmartTaskManagerDb`
- Confirm the source connection string still matches or intentionally overrides:
  `Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True`
- Confirm the app is not pointing at a different local or remote SQL instance through environment overrides.
- Confirm the core tables exist:
  `Users`, `Tasks`, `TaskHistoryEntries`
- Confirm `__EFMigrationsHistory` exists and contains the current migration set if the local database was created through EF Core migrations.

## 2. Provision The Target Azure SQL Environment

- Create the Azure resource group.
- Create the Azure SQL logical server.
- Add firewall rule for the current client IP.
- Add firewall rule for Azure services if App Service will connect later.
- Create one Azure SQL Database:
  - free offer if supported
  - otherwise the paid serverless fallback

Important:

- the target database must remain new or empty before import
- do not let the API start against the target before the import step

## 3. Freeze Writes

- Stop the local API if it is running.
- Stop the local web app if it is running.
- Close tools or scripts that may change task or user data.
- Treat the migration as offline for the duration of export and import.

## 4. Export The Local Database

- Create the `.bacpac` under:
  `C:\tmp\SmartTaskManagerDb.bacpac`
- Run `SqlPackage /Action:Export`
- Verify the export finishes successfully.
- Do not continue if export errors indicate unsupported objects or data issues.

## 5. Import Into Azure SQL Database

- Confirm again that the Azure target database is empty.
- Run `SqlPackage /Action:Import`
- Wait for import to complete.
- Do not allow the application to initialize the database during the import window.

## 6. Validate Schema, Data, And Migration History

- Check that the target contains:
  - `Users`
  - `Tasks`
  - `TaskHistoryEntries`
  - `__EFMigrationsHistory`
- Compare row counts between source and target for:
  - `Users`
  - `Tasks`
  - `TaskHistoryEntries`
- Spot-check a few representative rows:
  - one user
  - one task
  - one task history chain
- Confirm foreign-key relationships look correct.

## 7. Prepare The Application Cutover

- Build the final Azure SQL connection string:
  `Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=<database-name>;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
- Set the API runtime app setting later as:
  `ConnectionStrings__SmartTaskManager`
- Do not put this value into tracked source files.

## 8. Smoke Test The API

- Start the API against the Azure SQL connection string.
- Confirm startup succeeds.
- Confirm startup migration does not fail.
- Confirm the API can:
  - list users
  - list tasks
  - load task history
- Confirm no accidental reseeding occurs in production configuration.

## 9. Smoke Test The Full Application

- Start or deploy the web app pointing at the API.
- Sign in.
- Load dashboard and task screens.
- Create a low-risk test record.
- Confirm the new record appears in Azure SQL.

## 10. Rollback Plan

If validation fails:

- stop the API/web app from using Azure SQL
- switch the API connection string back to the local or previous database
- keep the original local `SmartTaskManagerDb` unchanged
- fix the export/import issue
- reprovision a fresh empty Azure SQL Database if necessary
- rerun the migration

## 11. Post-Cutover Hardening

- keep only the firewall rules you still need
- verify no secret or password was written to source control
- consider replacing SQL authentication with a stronger production access model later

## Final Reminder

- Do not rely on EF Core startup migration as the primary data migration strategy.
- Use BACPAC export/import as the primary data move.
- Keep the local database intact until Azure validation is complete.

## Execution Snapshot

Completed Azure-side steps:

- resource group created:
  `rg-smarttaskmanager-data-dev-weu`
- logical server created:
  `sql-stm-dev-weu-01`
- database created:
  `SmartTaskManagerDb`
- `.bacpac` imported successfully from:
  `C:\tmp\SmartTaskManagerDb.bacpac`

Validated target contents:

- tables present:
  `Users`, `Tasks`, `TaskHistoryEntries`, `__EFMigrationsHistory`
- row counts:
  - `Users`: `3`
  - `Tasks`: `9`
  - `TaskHistoryEntries`: `13`
  - `__EFMigrationsHistory`: `1`

Validated sample data:

- users include:
  `Alice`, `Bob`, `Carla`
- sample task titles include:
  `Archive old travel plan`, `Buy groceries`, `Complete LINQ exercises`
