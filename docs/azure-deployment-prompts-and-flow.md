# Azure Deployment Prompts And Flow

## Purpose

This document consolidates the six prompts prepared for this repository and explains the recommended order to use them.

The goal is to help you move this solution from local development to Azure with:

- Azure App Service for `SmartTaskManager.Web`
- Azure App Service for `SmartTaskManager.Api`
- Azure SQL Database for the API database
- Microsoft Entra client secret rotation for the web app registration

This file is intentionally repo-specific.

## Current Repository Facts

These are the important assumptions verified from the repository:

- There are two deployable applications:
  - `src/SmartTaskManager.Web`
  - `src/SmartTaskManager.Api`
- `SmartTaskManager.Web` is the server-side Blazor front end.
- `SmartTaskManager.Api` is the protected backend API.
- The API uses `ConnectionStrings:SmartTaskManager`.
- The local default database is `SmartTaskManagerDb` on local SQL Server unless your local machine uses a different override.
- The API applies EF Core migrations on startup.
- The web app expects the Entra secret through configuration, not from tracked source code.
- The placeholder currently present in source is:

```json
"ClientSecret": "__SET_IN_USER_SECRETS_OR_ENVIRONMENT__"
```

- The web app registration currently referenced in source is:
  - Tenant ID: `e099cebd-5eea-41a3-88db-bcb9a9cba83e`
  - Web Client ID: `ffdda8ba-1389-4fa9-bba5-b06d14ef55e5`
- The API registration currently referenced in source is:
  - API Client ID: `3bede5d9-a947-4d25-a3c1-54df15d5ed60`
  - API Audience: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60`
  - API Scope: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite`

## Important Safety Rules

- Do not write the real Entra client secret into `appsettings.json` or `appsettings.Production.json`.
- Do not commit any real secret or connection string password to source control.
- Use local user secrets for development.
- Use Azure App Service application settings for cloud runtime configuration.
- The old Entra client secret cannot be recovered. If you no longer have it, create a new one.
- For production on Azure, managed identity or certificates are better than client secrets. The prompts below keep client secrets because that is what your current code expects.

## Prompt Catalog

There are six prompts.

### Planning Prompts

These prompts inspect the repo, prepare docs and scripts, and do not make Azure changes:

1. Prompt 1: Prepare App Service deployment assets
2. Prompt 3: Prepare Azure SQL provisioning and local data migration
3. Prompt 5: Prepare Entra client secret rotation

### Execution Prompts

These prompts perform Azure changes later, after review:

1. Prompt 4: Provision Azure SQL and migrate local SQL Server data
2. Prompt 2: Deploy the applications to Azure App Service
3. Prompt 6: Rotate the Entra client secret and inject it into runtime configuration

## Recommended Flow

This is the recommended order for this repository.

### Phase 1: Generate The Planning Assets

Run these prompts first:

1. Prompt 1
2. Prompt 3
3. Prompt 5

Why this order:

- Prompt 1 prepares the Azure App Service deployment plan for the two web apps.
- Prompt 3 prepares the Azure SQL server, database, and local-data migration plan.
- Prompt 5 prepares the secret rotation plan for the Entra web app registration.

At the end of this phase, you should have draft documentation and helper scripts, but no Azure resources should exist yet.

### Phase 2: Review The Generated Docs

Before executing anything, review the outputs from the planning prompts.

Focus on:

- which App Service profile was recommended
  - `F1` free
  - `B1` cheapest paid dedicated tier
- which Azure SQL option was recommended
  - free Azure SQL Database offer if available
  - cheapest paid option if not available
- whether the migration plan chose `SqlPackage` BACPAC export/import
- which app settings must be applied to each app
- whether the prompt identified any SQL compatibility risks

You should also decide the final naming for:

- Azure resource group
- Azure App Service plan
- Web app name
- API app name
- Azure SQL logical server name
- Azure SQL database name

### Phase 3: Provision Azure SQL And Migrate Local Data

Run Prompt 4 next.

Why Prompt 4 comes before App Service deployment:

- `SmartTaskManager.Api` needs a valid SQL connection string.
- The API applies migrations on startup.
- It is simpler to deploy the API after the target Azure SQL database exists and your local data has already been migrated.

Expected outcomes of Prompt 4:

- an Azure resource group exists or is confirmed
- an Azure SQL logical server is created
- an Azure SQL Database is created
- firewall rules are created
- your local `SmartTaskManagerDb` is exported to a `.bacpac`
- the `.bacpac` is imported into Azure SQL Database
- validation is performed
- you get the final Azure SQL connection string for:
  - `ConnectionStrings__SmartTaskManager`

Important note:

- The API startup migration is not the main data migration strategy.
- The data migration should happen through the prepared database migration flow.

### Phase 4: Deploy The Web App And API To Azure App Service

Run Prompt 2 after Prompt 4.

Why Prompt 2 comes after Prompt 4:

- By now you already know the final Azure SQL connection string.
- The API app settings can be configured correctly the first time.
- The web app can point to the final deployed API URL instead of a temporary placeholder.

Expected outcomes of Prompt 2:

- an App Service plan is created
- the Web app is created
- the API app is created
- publish artifacts are produced with `dotnet publish`
- Azure App Service settings are applied
- the applications are deployed
- the docs are updated with real Azure names and URLs

Important note:

- Prompt 2 should configure the API connection string using the Azure SQL result from Prompt 4.
- Prompt 2 should not hardcode the Entra client secret in source files.

### Phase 5: Rotate The Entra Client Secret And Inject It Safely

Run Prompt 6 after Prompt 2.

Why Prompt 6 comes after Prompt 2:

- The Azure Web App must already exist before you can push `AzureAd__ClientSecret` into its App Service settings.
- The prompt needs the final Azure resource group and Web App name.

Expected outcomes of Prompt 6:

- existing Entra credential metadata is listed
- a new client secret is created with `--append`
- the new secret is stored locally with `.NET user-secrets`
- the new secret is stored in Azure App Service app settings on `SmartTaskManager.Web`
- the sign-in flow is validated
- the old secret can be removed after successful validation

Important note:

- Only `SmartTaskManager.Web` needs the client secret.
- `SmartTaskManager.Api` does not need `AzureAd__ClientSecret`.

### Phase 6: Validate End-To-End

After Prompts 4, 2, and 6 are done, validate the full flow:

1. Open the deployed web app.
2. Sign in with Microsoft Entra.
3. Confirm the web app can obtain delegated tokens.
4. Confirm the web app can call the deployed API.
5. Confirm the API can reach Azure SQL Database.
6. Confirm the migrated data appears correctly.
7. Confirm create/update flows still work.

### Phase 7: Cleanup And Hardening

After the deployment is stable:

- remove the old Entra secret by key ID
- confirm the Azure SQL firewall rules are only what you need
- confirm no real secrets were written to tracked files
- consider replacing the client secret with managed identity or certificate-based auth for production

## Short Version Of The Order

Use the prompts in this order:

1. Prompt 1
2. Prompt 3
3. Prompt 5
4. Review docs and choose names, region, SKUs
5. Prompt 4
6. Prompt 2
7. Prompt 6
8. Final validation
9. Cleanup old Entra secret

## Why This Order Is Best For This Repo

This repo has three dependencies that matter:

1. The API depends on a working SQL connection string.
2. The Web app depends on a working API URL.
3. The Web app sign-in flow depends on a valid Entra client secret at runtime.

That means:

- database first
- app deployment second
- secret injection after the web app exists

The only exception is local testing. If you want to test the sign-in flow locally before deploying Azure Web Apps, you can create a new secret earlier and set only local user secrets. But for the full cloud runtime flow, Prompt 6 should still be run after Prompt 2 so the Azure Web App setting can be applied.

## Runtime Settings That Matter

### SmartTaskManager.Web

These settings matter for the deployed web app:

- `AzureAd__Instance`
- `AzureAd__TenantId`
- `AzureAd__ClientId`
- `AzureAd__ClientSecret`
- `AzureAd__CallbackPath`
- `AzureAd__SignedOutCallbackPath`
- `SmartTaskManagerApi__BaseUrl`
- `SmartTaskManagerApi__Audience`
- `SmartTaskManagerApi__Scopes`

### SmartTaskManager.Api

These settings matter for the deployed API:

- `ConnectionStrings__SmartTaskManager`
- `AzureAd__Instance`
- `AzureAd__TenantId`
- `AzureAd__ClientId`
- `AzureAd__Audience`
- `Authorization__RequiredScope`

## What Each Prompt Should Produce

### Prompt 1

Expected outputs:

- App Service deployment option docs
- App Service commands docs
- App Service Entra/settings docs
- optional Bicep or PowerShell deployment helpers

### Prompt 3

Expected outputs:

- Azure SQL options docs
- Azure SQL provision and migration command docs
- Azure SQL cutover checklist
- optional PowerShell migration helpers

### Prompt 5

Expected outputs:

- Entra client secret rotation docs
- Entra client secret command docs
- optional PowerShell secret rotation helpers

## How To Use Each Prompt

Use the prompts in a fresh agent turn when possible.

Recommended approach:

1. Keep the repo root as the working directory.
2. Paste one prompt at a time.
3. Let the agent finish creating files.
4. Review the generated docs before moving to the next prompt.
5. Do not jump to execution prompts until the planning prompts are reviewed.

## Prompt 1: Prepare The Deployment Assets, But Do Not Deploy

```text
You are in the root of a .NET solution. Do not deploy anything yet. Do not create Azure resources. Do not run Azure CLI commands that change cloud state. Only inspect the repository, prepare deployment assets, and write documentation.

Repository expectations to validate from source:
- There are two deployable apps:
  - src/SmartTaskManager.Web
  - src/SmartTaskManager.Api
- SmartTaskManager.Web is a .NET 10 Blazor Web App using server interactivity and Microsoft Entra ID OpenID Connect.
- SmartTaskManager.Api is a .NET 10 ASP.NET Core Web API using Microsoft Entra JWT bearer auth.
- The web app calls the API through configuration settings.
- The API requires a SQL Server connection string named SmartTaskManager.
- The API auto-applies EF Core migrations on startup.
- The web app requires AzureAd:ClientSecret to be injected securely and never committed.

Cost target:
- Produce two deployment profiles:
  1. Free/dev-test profile using Azure App Service Free F1 if viable.
  2. Lowest paid profile using the cheapest dedicated App Service tier, preferably B1.
- Prefer one shared App Service Plan for both Web Apps in the paid profile to minimize cost.
- Do not use containers unless the repo proves they are required.
- Do not create Azure SQL unless explicitly asked. Document where the SQL connection string must be provided.

Technical constraints:
- This repo has two separate web projects, so publish and deploy them separately.
- Do not rely on a single multi-project source deployment from repo root.
- Prefer explicit dotnet publish commands for each app in Release mode.
- Treat the free profile as dev/test only.
- If the free profile needs to stay on the default *.azurewebsites.net hostnames, document that clearly.
- Include Microsoft Entra redirect URI and signed-out callback updates for the deployed web app URL.
- Document whether WebSockets, ARR affinity, and Always On matter for the Blazor app in each profile.

Create these markdown files:
1. docs/azure-app-service-deployment-options.md
- Compare the Free F1 profile versus the paid B1 profile for this repo.
- Explain the tradeoffs, limitations, and which profile you recommend.
- Describe the Azure resources needed.
- State whether one or two App Service Plans are needed for the free profile, and why.

2. docs/azure-app-service-commands.md
- Provide step-by-step Azure CLI commands.
- Include sections for:
  - prereqs
  - free/dev-test profile
  - paid B1 profile
  - app settings
  - deployment commands
  - verification commands
  - cleanup commands
- Include commands for dotnet publish, az group create, az appservice plan create, az webapp create, az webapp config appsettings set, deployment, log tailing, and cleanup.
- Do not execute any of them.

3. docs/azure-app-service-entra-and-settings.md
- List the exact app settings keys and placeholder values required by each app.
- Include the Entra redirect URIs and logout callback URIs that must be added.
- Include where the SQL connection string must be set.
- Note that the API migration behavior happens on startup once the connection string is valid.

Use and verify these settings keys unless the code proves otherwise:
- Web app:
  - AzureAd__Instance
  - AzureAd__TenantId
  - AzureAd__ClientId
  - AzureAd__ClientSecret
  - AzureAd__CallbackPath
  - AzureAd__SignedOutCallbackPath
  - SmartTaskManagerApi__BaseUrl
  - SmartTaskManagerApi__Audience
  - SmartTaskManagerApi__Scopes
- API app:
  - ConnectionStrings__SmartTaskManager
  - AzureAd__Instance
  - AzureAd__TenantId
  - AzureAd__ClientId
  - AzureAd__Audience
  - Authorization__RequiredScope

Optional but preferred outputs:
- infra/appservice.bicep
- scripts/deploy-appservice.ps1
Do not execute them.

Important:
- If you find any setting names differ from the list above, update the docs to match the code.
- Do not modify application code unless a deployment blocker is proven from the repository.
- End with a short summary of what you created, what still needs user input, and what would be executed later after approval.
```

## Prompt 2: Execute Later, After Review

```text
Use the deployment assets and markdown files already prepared in this repository for Azure App Service deployment.

Now deploy, but only after showing me the final plan first.

Rules:
- First show the exact profile you intend to use:
  - free-f1
  - paid-b1
- Show the exact Azure resources that will be created:
  - resource group
  - app service plan
  - web app for SmartTaskManager.Web
  - web app for SmartTaskManager.Api
- Show the exact region, SKU, app names, and expected monthly cost category.
- If any required input is missing, stop and ask only for the missing values.

Before deployment, verify:
- the repo still contains the same two deployable apps
- the generated markdown files are still accurate
- the Entra redirect URIs to be added
- the SQL connection string source
- the exact app settings to apply

During deployment:
- use explicit dotnet publish commands for each app
- deploy the Web and API separately
- apply the required app settings
- keep secrets out of source control
- do not create Azure SQL unless I explicitly ask
- if the database connection string is missing, stop before API validation and document the blocker

After deployment:
- validate the HTTPS URL of each app
- validate the web app can reach the API
- validate the API startup and migration behavior
- update these markdown files with actual deployed names, URLs, and commands used:
  - docs/azure-app-service-deployment-options.md
  - docs/azure-app-service-commands.md
  - docs/azure-app-service-entra-and-settings.md
- add a final section with rollback and cleanup commands
- return a concise deployment summary with any remaining manual Azure portal steps
```

## Prompt 3: Prepare Azure SQL Provisioning And Local Data Migration, But Do Not Execute

```text
You are in the root of a .NET solution. Do not deploy anything yet. Do not create Azure resources yet. Do not export or import any database yet. Only inspect the repository, verify assumptions, choose the lowest-cost Azure SQL option, and generate deployment/migration documentation and scripts.

Goal:
Prepare everything needed to:
1. Provision a new Azure SQL logical server and Azure SQL Database for this project.
2. Migrate the actual local SQL Server database from this laptop to Azure SQL Database.
3. Update the app configuration later to use Azure SQL Database.

Repository facts to verify from source:
- The API project is src/SmartTaskManager.Api.
- The API uses a SQL Server connection string named ConnectionStrings:SmartTaskManager.
- The current local default database is SmartTaskManagerDb on localhost unless code/config proves otherwise.
- The API auto-applies EF Core migrations on startup.
- The Web app calls the API separately and does not directly connect to SQL Server.

Local source database assumption to validate:
- Current source connection string is expected to be:
  Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True

Cost target:
- Prefer Azure SQL Database free offer if the subscription and region support it.
- If the free offer is not available, choose the lowest-cost Azure SQL Database option that is suitable for this app and for BACPAC import.
- Explain the exact SKU chosen and why.
- Keep the design as cheap as possible.

Migration strategy:
- Primary strategy: offline migration using SqlPackage export/import with a .bacpac file.
- First assess whether this database is a good fit for Azure SQL Database and whether SqlPackage is appropriate.
- If you detect blockers, large-size risk, or unsupported SQL Server features, document a fallback path using Azure Data Studio + Azure SQL migration extension or Azure Database Migration Service offline migration.
- Do not execute the migration.

Technical requirements:
- Treat “Azure SQL Server” as an Azure SQL logical server.
- Plan to create:
  - one resource group
  - one Azure SQL logical server
  - one single Azure SQL Database
  - server firewall rules for:
    - the current client public IP
    - Azure services access if needed for App Service connectivity
- Generate the future target connection string in Azure SQL format with Encrypt=True and TrustServerCertificate=False.
- Do not store secrets in source control.

Create these markdown files:
1. docs/azure-sql-options.md
- Compare:
  - Azure SQL Database free offer if available
  - the cheapest paid Azure SQL Database option if free is unavailable
- State the selected recommendation.
- Explain tradeoffs, limits, and expected fit for this SmartTaskManager app.

2. docs/azure-sql-provision-and-migrate-commands.md
- Provide step-by-step commands, but do not run them.
- Include sections for:
  - prerequisites
  - provisioning Azure SQL logical server
  - provisioning Azure SQL Database
  - firewall rules
  - local database export to .bacpac
  - import to Azure SQL Database
  - validation queries and checks
  - updating the API app setting later
  - cleanup commands
- Prefer Azure CLI for Azure resources and SqlPackage for export/import.
- Include PowerShell-friendly command examples.

3. docs/azure-sql-cutover-checklist.md
- Include a clear cutover checklist:
  - verify source DB
  - freeze writes during offline migration
  - export
  - import
  - validate row counts and critical tables
  - switch app connection string
  - smoke test API
  - rollback steps

Optional but preferred files:
- scripts/provision-azure-sql.ps1
- scripts/migrate-local-sql-to-azure-sql.ps1
- scripts/validate-azure-sql-migration.ps1
Do not execute them.

Command guidance to document:
- Azure resource creation should use:
  - az group create
  - az sql server create
  - az sql server firewall-rule create
  - az sql db create
- Database movement should prefer:
  - SqlPackage /Action:Export
  - SqlPackage /Action:Import
- Use a temporary local .bacpac path under C:\tmp unless a better path is justified.

Validation requirements:
- Verify whether importing into a new/empty Azure SQL Database is required.
- Verify that the migration preserves schema, data, and EF migration history.
- Document that the API startup migration should not be relied on as the primary data migration path.
- Document any unsupported SQL Server features only if you actually detect them.

Important:
- Do not modify application code unless you prove a real Azure SQL compatibility blocker in the repository.
- End with a short summary of what you created, what still needs user input, and what exact commands would be run later after approval.
```

## Prompt 4: Execute Azure SQL Provisioning And Local Data Migration Later, After Review

```text
Use the Azure SQL planning documents and scripts already prepared in this repository.

Before any Azure execution, first prepare the local offline export manually in SSMS.

Pre-step in SSMS:
1. Connect to `(localdb)\MSSQLLocalDB` with `Autenticacion de Windows`.
2. In `Explorador de objetos`, expand `Bases de datos`.
3. Right-click `SmartTaskManagerDb`.
4. Choose `Tareas`.
5. Choose `Exportar aplicacion de nivel de datos...`
6. In the wizard:
   - `Introduccion`: `Siguiente`
   - `Configuracion de exportacion`: save to local disk
   - File path: `C:\tmp\SmartTaskManagerDb.bacpac`
7. Start the export and wait for `Correcto` / success.
8. Confirm the file exists at `C:\tmp\SmartTaskManagerDb.bacpac`.

Important for the pre-step:
- Close the app first so writes stop during export.
- Do not use `Generar scripts`; use `Exportar aplicacion de nivel de datos`.
- Do not change the database name.
- Treat this as an offline export with acceptable downtime.

If the `.bacpac` file is not ready yet, stop and tell me to complete the SSMS export first.
When it finishes, I will send:
`bacpac ready`

Only after that, continue with Azure execution.

Now provision Azure SQL and migrate the local SQL Server database, but only after showing me the final plan first.

Rules:
- First show:
  - selected Azure SQL option
  - exact resource group name
  - exact Azure SQL logical server name
  - exact database name
  - exact region
  - exact pricing/SKU choice
  - whether the free offer is being used
- If any required input is missing, stop and ask only for the missing values.

Before execution, verify:
- the repository still uses ConnectionStrings:SmartTaskManager
- the local source database name and source instance
- the prepared markdown files are still accurate
- the chosen migration path is still the best fit
- the local machine has SqlPackage available, or install path instructions are already documented
- the file `C:\tmp\SmartTaskManagerDb.bacpac` exists before starting the Azure import

Execution sequence:
1. Create or confirm the Azure resource group.
2. Create the Azure SQL logical server.
3. Create firewall rules for:
   - the current client public IP
   - Azure services access if needed for App Service connectivity
4. Create the Azure SQL Database with the selected lowest-cost or free option.
5. Use the existing `.bacpac` file at `C:\tmp\SmartTaskManagerDb.bacpac` as the source artifact.
6. Import the `.bacpac` into Azure SQL Database.
7. Validate the migration with row counts, schema checks, and spot checks on important tables.
8. Produce the final Azure SQL connection string for the API.
9. Update these markdown files with the actual values used:
   - docs/azure-sql-options.md
   - docs/azure-sql-provision-and-migrate-commands.md
   - docs/azure-sql-cutover-checklist.md

Safety rules:
- Treat this as an offline migration with acceptable downtime.
- Do not delete the local database.
- Do not overwrite an existing Azure SQL Database unless I explicitly approve it.
- If the target database is not empty and import would conflict, stop and explain.
- If unsupported SQL Server features or import blockers are found, stop and switch to the documented fallback plan instead of guessing.

After migration:
- show the final Azure SQL logical server name and database name
- show the final API connection string with the password redacted
- show the exact app setting key to use later:
  ConnectionStrings__SmartTaskManager
- summarize validation results and any remaining manual steps
```

## Prompt 5: Prepare Client Secret Rotation, But Do Not Execute

```text
You are in the root of a .NET solution. Do not execute any Azure changes yet. Do not create a secret yet. Do not modify tracked source files to contain any real secret values. Only inspect the repository, verify the current configuration pattern, and generate documentation and scripts for rotating the Microsoft Entra client secret safely.

Repository facts to verify from source:
- The web app project is src/SmartTaskManager.Web.
- The web app currently expects:
  - AzureAd:Instance
  - AzureAd:TenantId
  - AzureAd:ClientId
  - AzureAd:ClientSecret
  - AzureAd:CallbackPath
  - AzureAd:SignedOutCallbackPath
- The placeholder value is:
  __SET_IN_USER_SECRETS_OR_ENVIRONMENT__
- The project currently validates that the real client secret must come from user secrets or environment variables.
- The API project does not require this client secret.

Known values to confirm from source:
- Web app registration client ID: ffdda8ba-1389-4fa9-bba5-b06d14ef55e5
- Tenant ID: e099cebd-5eea-41a3-88db-bcb9a9cba83e

Goal:
Prepare a safe client secret rotation flow that:
1. Creates a new Microsoft Entra client secret for the SmartTaskManager.Web app registration.
2. Stores it locally using .NET user secrets for development.
3. Stores it in Azure App Service application settings for the deployed web app.
4. Never writes the real secret into:
   - src/SmartTaskManager.Web/appsettings.json
   - src/SmartTaskManager.Web/appsettings.Production.json
   - any tracked file in the repository

Use Azure CLI for the Entra secret rotation:
- az ad app credential list
- az ad app credential reset
- az ad app credential delete

Important safety requirements:
- Document that the old secret value cannot be retrieved.
- Use az ad app credential reset with --append first, to avoid breaking running environments before cutover is validated.
- Capture the new secret into a shell variable instead of printing it repeatedly.
- Use:
  - dotnet user-secrets set "AzureAd:ClientSecret" ... for local development
  - AzureAd__ClientSecret in Azure Web App app settings for Azure runtime
- Do not set the secret on the API web app.
- Do not commit the secret to source control.

Create these files:
1. docs/entra-client-secret-rotation.md
- Explain:
  - why the old secret cannot be recovered
  - how to create a new one
  - how to set it locally
  - how to set it in Azure App Service
  - how to validate the sign-in flow
  - how to remove the old secret after validation

2. docs/entra-client-secret-commands.md
- Include PowerShell-friendly commands for:
  - listing existing credential metadata
  - creating a new secret with --append
  - storing the new secret in a PowerShell variable
  - setting local user secrets
  - setting Azure Web App app settings
  - restarting the web app if needed
  - deleting the old secret by key ID after successful validation

3. scripts/rotate-entra-client-secret.ps1
- Generate a script that:
  - accepts the web app registration client ID
  - creates a new secret with --append
  - stores the result in memory only
  - writes the secret to local user secrets
  - optionally writes the secret to Azure App Service app settings if resource group and web app name are provided
- Do not execute it.

4. scripts/remove-old-entra-client-secret.ps1
- Generate a script that removes an old credential by key ID after cutover validation.
- Do not execute it.

Exact settings to use:
- Local dev:
  dotnet user-secrets set "AzureAd:ClientSecret" "<new-secret>" --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
- Azure App Service:
  AzureAd__ClientSecret=<new-secret>

Do not modify application code unless you prove the code is incompatible with this runtime-secret pattern.

End with a short summary of what you created, what values still need user input, and what exact commands would be run later after approval.
```

## Prompt 6: Execute Client Secret Rotation And Runtime Injection Later

```text
Use the client secret rotation documents and scripts already prepared in this repository.

Now rotate the Microsoft Entra client secret for SmartTaskManager.Web and inject it into the correct runtime configuration locations, but only after showing me the final plan first.

Rules:
- First show:
  - confirmed web app registration client ID
  - confirmed tenant ID
  - whether only SmartTaskManager.Web uses the secret
  - exact Azure Web App name that will receive the secret
  - exact Azure resource group
- If any required value is missing, stop and ask only for the missing values.

Before execution, verify from source:
- src/SmartTaskManager.Web still expects AzureAd:ClientSecret
- the placeholder is still __SET_IN_USER_SECRETS_OR_ENVIRONMENT__
- the project path for local user secrets is still:
  .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
- the API does not require a client secret

Execution sequence:
1. List existing credential metadata for the app registration:
   az ad app credential list --id ffdda8ba-1389-4fa9-bba5-b06d14ef55e5
2. Create a new client secret safely with append:
   az ad app credential reset --id ffdda8ba-1389-4fa9-bba5-b06d14ef55e5 --append --display-name "SmartTaskManager.Web rotated secret" --years 1
3. Capture the new secret into a variable without echoing it again in logs.
4. Set the local development secret using:
   dotnet user-secrets set "AzureAd:ClientSecret" "<new-secret>" --project .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj
5. Set the Azure App Service app setting on the web frontend only:
   az webapp config appsettings set --resource-group <resource-group> --name <web-app-name> --settings AzureAd__ClientSecret="<new-secret>"
6. Do not set the secret on the API web app.
7. Do not write the real secret into appsettings.json or appsettings.Production.json.
8. Restart the web app only if required for the new setting to be picked up.
9. Validate the web sign-in flow and downstream API token acquisition.
10. After successful validation, list credential metadata again and remove the old secret by key ID if instructed.

Safety rules:
- Do not delete all old credentials before validation.
- Do not print the full secret more than necessary.
- Do not commit the secret into source control.
- If Azure CLI output suggests the app registration is different than the repo configuration, stop and explain.

After execution:
- update these files with the actual redacted values and commands used:
  - docs/entra-client-secret-rotation.md
  - docs/entra-client-secret-commands.md
- show the exact local command used
- show the exact Azure Web App app setting key used
- confirm whether old credentials still exist and whether cleanup is still pending
```

## Suggested User Inputs To Prepare In Advance

Before you run the execution prompts, have these values ready:

- Azure subscription
- Azure region
- resource group name
- App Service plan name
- Web app name
- API app name
- Azure SQL logical server name
- Azure SQL database name
- Azure SQL admin login name
- Azure SQL admin password
- your local SQL Server source database confirmation

## Practical Notes

### If You Want The Cheapest Path

Use the planning prompts first and let them evaluate:

- App Service `F1` versus `B1`
- Azure SQL free offer versus lowest paid option

For this repo, the practical low-cost pattern usually ends up being:

- Azure SQL first
- App Service `B1` for both apps in one shared plan if you want the cleanest dedicated setup
- client secret injected only at runtime

### If You Want To Stay On Free Tiers As Much As Possible

Expect tradeoffs:

- App Service `F1` is dev/test only and has quota limits
- Azure SQL free offer availability depends on subscription and region
- sign-in, API traffic, and the server-side Blazor model may be constrained by the free tier

## Final Reminder

Do not copy the real Entra client secret into your application code.

For this repository, the correct places are:

- local development:
  - `.NET user secrets`
- Azure runtime:
  - Azure App Service application settings on `SmartTaskManager.Web`

The placeholder in source should remain as-is.
