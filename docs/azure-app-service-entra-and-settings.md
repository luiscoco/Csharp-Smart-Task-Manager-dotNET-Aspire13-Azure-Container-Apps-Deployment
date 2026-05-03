# Azure App Service Entra And Settings

## Purpose

This document lists the exact Azure App Service settings and Microsoft Entra redirect values required by this repository.

The current codebase already contains the configuration model. Azure runtime settings must override only the values that differ in Azure, especially secrets and URLs.

## Validated Configuration Model

### `SmartTaskManager.Web`

The web app binds:

- `AzureAd`
- `SmartTaskManagerApi`

The web app validates at startup that:

- `AzureAd:ClientSecret` is not the placeholder
- `AzureAd:CallbackPath` starts with `/`
- `AzureAd:SignedOutCallbackPath` starts with `/`

### `SmartTaskManager.Api`

The API actively reads:

- `ConnectionStrings:SmartTaskManager`
- `AzureAd`

The API startup pipeline applies EF Core migrations once the connection string is valid and the target database is reachable.

The current codebase still carries `Authorization:RequiredScope` in configuration, and it was deployed as an App Service setting for consistency, but the current API code does not actively consume that setting.

## Web App Settings

These settings are required for `SmartTaskManager.Web`.

| Key | Expected value / placeholder | Notes |
| --- | --- | --- |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` | Already present in source defaults. |
| `AzureAd__TenantId` | `e099cebd-5eea-41a3-88db-bcb9a9cba83e` | Web app tenant. |
| `AzureAd__ClientId` | `ffdda8ba-1389-4fa9-bba5-b06d14ef55e5` | Web app registration client ID. |
| `AzureAd__ClientSecret` | `<set-in-app-service-only>` | Never commit this value. |
| `AzureAd__CallbackPath` | `/signin-oidc` | Must remain a relative path. |
| `AzureAd__SignedOutCallbackPath` | `/signout-callback-oidc` | Must remain a relative path. |
| `SmartTaskManagerApi__BaseUrl` | `https://<api-app-name>.azurewebsites.net/` | Must include trailing slash behavior; the app also normalizes it. |
| `SmartTaskManagerApi__Audience` | `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60` | Downstream API audience. |
| `SmartTaskManagerApi__Scopes` | `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite` | Delegated scope list string. |

### Web Secret Handling

Current placeholder in source:

```json
"ClientSecret": "__SET_IN_USER_SECRETS_OR_ENVIRONMENT__"
```

Leave that placeholder in tracked files.

The real value must go into:

- local development:
  `.NET user secrets`
- Azure runtime:
  App Service setting `AzureAd__ClientSecret`

## API App Settings

These settings are required for `SmartTaskManager.Api`.

| Key | Expected value / placeholder | Notes |
| --- | --- | --- |
| `ConnectionStrings__SmartTaskManager` | `Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<sql-database>;Persist Security Info=False;User ID=<sql-admin>;Password=<sql-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` | Required before the API can start cleanly in Azure. |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` | Authority base. |
| `AzureAd__TenantId` | `e099cebd-5eea-41a3-88db-bcb9a9cba83e` | Tenant ID. |
| `AzureAd__ClientId` | `3bede5d9-a947-4d25-a3c1-54df15d5ed60` | API app registration client ID. |
| `AzureAd__Audience` | `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60` | Recommended explicitly, though code can infer it from `ClientId` if omitted. |
| `Authorization__RequiredScope` | `Tasks.ReadWrite` | Required delegated scope name. |

### Optional Production API Toggles

These are not mandatory for the prompt, but they align with the committed production settings:

| Key | Suggested value |
| --- | --- |
| `Database__EnableEfLogging` | `false` |
| `Database__EnableDetailedErrors` | `false` |
| `Database__EnableSensitiveDataLogging` | `false` |
| `Seeding__EnableSampleData` | `false` |

## Microsoft Entra Redirect URIs

### Default Azure Hostname Pattern

If the web app is deployed as:

```text
https://<web-app-name>.azurewebsites.net
```

then add these redirect values to the `SmartTaskManager.Web` app registration:

- sign-in redirect URI:
  `https://<web-app-name>.azurewebsites.net/signin-oidc`
- sign-out callback URI:
  `https://<web-app-name>.azurewebsites.net/signout-callback-oidc`

### If You Later Add A Custom Domain

Add the same paths for the custom domain:

- `https://<custom-domain>/signin-oidc`
- `https://<custom-domain>/signout-callback-oidc`

## Free Tier Notes

If you use the Free profile:

- keep the web app on the default `azurewebsites.net` hostname
- do not assume a custom-domain flow
- use the default Azure HTTPS hostname for Entra redirect configuration

This is important because the Free profile is a dev/test path only.

## Where The SQL Connection String Must Be Set

The API requires the SQL connection string at runtime.

Set it on the API web app as:

```text
ConnectionStrings__SmartTaskManager
```

Do not set it on `SmartTaskManager.Web`.

## Why The Database Must Exist Before Final API Validation

The API startup path calls the database initializer and runs EF Core migrations automatically.

That means:

- the database server must already be reachable
- the database name and credentials must already be valid
- final API smoke testing should happen only after the SQL connection string is configured

## Summary

Required later:

- web app secret:
  `AzureAd__ClientSecret`
- API connection string:
  `ConnectionStrings__SmartTaskManager`
- web redirect URI update in Entra:
  `https://<web-app-name>.azurewebsites.net/signin-oidc`
- web sign-out callback update in Entra:
  `https://<web-app-name>.azurewebsites.net/signout-callback-oidc`

## Deployed Snapshot 2026-04-30

Actual deployed app names and URLs:

- web app: `stm-web-dev-weu-e5bd93`
- web URL: `https://stm-web-dev-weu-e5bd93.azurewebsites.net`
- API app: `stm-api-dev-weu-e5bd93`
- API URL: `https://stm-api-dev-weu-e5bd93.azurewebsites.net`

Actual SQL target:

- logical server: `sql-stm-dev-weu-01`
- database: `SmartTaskManagerDb`
- deployed API connection string:
  `Server=tcp:sql-stm-dev-weu-01.database.windows.net,1433;Initial Catalog=SmartTaskManagerDb;Persist Security Info=False;User ID=stmsqladmin;Password=<redacted>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`

Actual web app settings applied:

- `AzureAd__Instance`
- `AzureAd__TenantId`
- `AzureAd__ClientId`
- `AzureAd__ClientSecret`
- `AzureAd__CallbackPath`
- `AzureAd__SignedOutCallbackPath`
- `SmartTaskManagerApi__BaseUrl`
- `SmartTaskManagerApi__Audience`
- `SmartTaskManagerApi__Scopes`

Actual API app settings applied:

- `ConnectionStrings__SmartTaskManager`
- `AzureAd__Instance`
- `AzureAd__TenantId`
- `AzureAd__ClientId`
- `AzureAd__Audience`
- `Authorization__RequiredScope`
- `Database__EnableEfLogging`
- `Database__EnableDetailedErrors`
- `Database__EnableSensitiveDataLogging`
- `Seeding__EnableSampleData`

Actual Microsoft Entra redirect URIs now present on `SmartTaskManager.Web`:

- `https://localhost:5001/signout-callback-oidc`
- `https://localhost:5001/signin-oidc`
- `https://stm-web-dev-weu-e5bd93.azurewebsites.net/signout-callback-oidc`
- `https://stm-web-dev-weu-e5bd93.azurewebsites.net/signin-oidc`

Actual secret handling used:

- the tracked placeholder stayed unchanged in source files
- the real `AzureAd__ClientSecret` value was read from local `.NET user-secrets`
- the real `AzureAd__ClientSecret` value was written to Azure App Service only
- no real secret value was committed into the repository

Validation notes:

- the web app is running and redirects to Microsoft Entra sign-in
- the API is running and returns `401` on `/api/users`, which confirms startup completed after the migration path
- a non-interactive delegated token test from Azure CLI hit `consent_required`, so the final end-to-end browser sign-in and UI data-load test still needs an interactive user session
