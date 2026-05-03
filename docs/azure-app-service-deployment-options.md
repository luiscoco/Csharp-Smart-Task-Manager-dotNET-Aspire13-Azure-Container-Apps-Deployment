# Azure App Service Deployment Options

## Scope

This document compares the two lowest-cost Azure App Service profiles for this repository:

- `F1` Free dev/test
- `B1` Basic paid dedicated

The comparison is based on the current solution shape:

- `src/SmartTaskManager.Web`
- `src/SmartTaskManager.Api`

Both applications are separate deployable web apps and should be published and deployed independently.

## Validated Repository Facts

The current codebase confirms the following:

- `SmartTaskManager.Web` is a `.NET 10` Blazor Web App using server interactivity.
- The web app uses Microsoft Entra ID with OpenID Connect sign-in.
- The web app acquires delegated tokens server-side and forwards them to the API.
- `SmartTaskManager.Api` is a `.NET 10` ASP.NET Core Web API.
- The API validates Microsoft Entra JWT bearer tokens.
- The web app reads the API URL from configuration.
- The API requires `ConnectionStrings:SmartTaskManager`.
- The API applies EF Core migrations on startup.
- The web app requires `AzureAd:ClientSecret` from user secrets or environment variables and rejects the placeholder at startup.

## Recommendation

Recommended profile: `B1` paid profile with one shared App Service Plan for both apps.

Why:

- It is the cheapest dedicated App Service tier.
- Both apps can share one paid plan, which minimizes cost while still avoiding the Free tier quota limits.
- The Blazor server-interactive front end is a better fit on a dedicated plan because it relies on SignalR-style real-time connections and benefits from stable warm runtime behavior.
- It keeps the deployment model simple: one resource group, one Windows App Service Plan, one Web App for the UI, one Web App for the API.

## Option 1: `F1` Free Dev/Test Profile

### When It Fits

Use this profile only when all of the following are true:

- you want the cheapest possible cloud proof-of-concept
- you accept quota limits and cold starts
- you do not need a custom domain
- you keep both apps on the default `*.azurewebsites.net` hostnames
- you treat the environment as dev/test, not production

### Benefits

- no App Service compute charge
- simplest way to validate that both apps can run in Azure
- built-in HTTPS on `*.azurewebsites.net` hostnames is sufficient for Entra redirect URIs

### Constraints

- Free tier is intended only for development and testing.
- The tier applies CPU, memory, bandwidth, and filesystem quotas per app.
- If quota is exceeded, the app can stop serving requests until quota resets.
- No scale-out.
- The front end can be unloaded during inactivity, which causes cold starts and reconnect friction for a server-interactive Blazor app.
- Do not plan around custom domains on this tier.

### Impact On This Repository

For `SmartTaskManager.Web`:

- viable for smoke testing and demos
- not ideal for long-lived interactive sessions
- WebSockets should still be enabled
- ARR affinity is not important on a single instance, but keeping client affinity enabled is harmless and future-proof
- Always On should not be assumed for the Free profile, so idle unload and warm-up latency must be expected

For `SmartTaskManager.Api`:

- viable for low-traffic dev/test
- still requires a valid SQL connection string
- startup migrations still run, so the database must already exist and be reachable

### Free Profile Resource Shape

Recommended shape:

- `1` resource group
- `1` Windows App Service Plan on `F1`
- `1` Web App for `SmartTaskManager.Web`
- `1` Web App for `SmartTaskManager.Api`

### One Or Two Plans For Free?

One plan is sufficient and recommended.

Why:

- multiple apps can run in the same App Service Plan
- both apps are low-scale by design in the Free profile
- using one plan keeps the configuration simpler
- there is no repository-driven need to isolate them onto separate plans

Two free plans are not required for this repository. Separate plans would only be justified if you deliberately want operational separation later.

## Option 2: `B1` Basic Paid Profile

### When It Fits

Use this profile when:

- you want the lowest-cost dedicated App Service setup
- you want a more stable runtime for the Blazor server app
- you want to reduce the risk of quota-based interruptions
- you want the cleanest path for repeatable testing and later hardening

### Benefits

- dedicated compute at the App Service Plan level
- both apps can share the same plan with no extra plan charge
- better fit for the interactive Blazor front end
- supports enabling warm-runtime behavior through Always On
- supports custom-domain and TLS feature growth later if needed

### Constraints

- it is not free
- both apps share the same dedicated plan resources, so they still influence each other
- if you later need independent scale or isolation, split them into separate plans

### Impact On This Repository

For `SmartTaskManager.Web`:

- recommended profile
- enable WebSockets
- keep client affinity enabled
- enable Always On

For `SmartTaskManager.Api`:

- recommended profile
- Always On is helpful to reduce cold-start behavior
- does not need WebSockets

### Paid Profile Resource Shape

Recommended shape:

- `1` resource group
- `1` Windows App Service Plan on `B1`
- `1` Web App for `SmartTaskManager.Web`
- `1` Web App for `SmartTaskManager.Api`

This is the lowest-cost dedicated shape that still keeps the deployment architecture clean.

## WebSockets, ARR Affinity, And Always On

### `SmartTaskManager.Web`

This app uses server interactivity, so treat it like a SignalR-backed interactive app.

- WebSockets:
  required in practice and should be enabled
- ARR affinity / session affinity:
  required when there is more than one backend instance
  on a single-instance deployment it is not critical, but leaving client affinity enabled is still safe
- Always On:
  important on the paid profile to reduce unload/warm-up issues
  do not depend on equivalent warm behavior in the Free profile

### `SmartTaskManager.Api`

- WebSockets:
  not required
- ARR affinity:
  not relevant for normal stateless API behavior
- Always On:
  optional but useful on the paid profile to reduce cold starts

## Custom Domain And Entra Redirect Implications

For the Free profile:

- keep the apps on `https://<app-name>.azurewebsites.net`
- register the web redirect URI against the default Azure hostname
- do not plan around a custom domain

For the paid `B1` profile:

- the default Azure hostname still works and is the simplest starting point
- you can add a custom domain later if desired

## Required Azure Resources

These resources are needed regardless of profile:

- `1` Azure resource group
- `1` App Service Plan
- `1` App Service Web App for `SmartTaskManager.Web`
- `1` App Service Web App for `SmartTaskManager.Api`

Outside App Service, but still required for the full solution:

- a reachable SQL Server-compatible database for the API connection string
- Microsoft Entra app registration values already referenced by the repo

This prompt does not create Azure SQL. It only documents where that connection string must be supplied later.

## Final Decision

Choose `F1` only for temporary dev/test validation on default Azure hostnames.

Choose `B1` for the recommended deployment path for this repository.

## Execution Snapshot 2026-04-30

Actual deployment choice:

- profile: `B1`
- subscription: `Subscription 2` (`e5bd93f3-dcd9-4833-a589-82e16245997c`)
- region: `West Europe`
- resource group: `rg-smarttaskmanager-data-dev-weu`
- app service plan: `asp-stm-dev-weu-b1`
- web app: `stm-web-dev-weu-e5bd93`
- API app: `stm-api-dev-weu-e5bd93`

Actual deployed URLs:

- web: `https://stm-web-dev-weu-e5bd93.azurewebsites.net`
- API: `https://stm-api-dev-weu-e5bd93.azurewebsites.net`

Actual supporting data source:

- Azure SQL logical server: `sql-stm-dev-weu-01`
- Azure SQL database: `SmartTaskManagerDb`

Observed results:

- the shared `B1` Windows App Service plan deployed successfully
- the web app is running and redirects into Microsoft Entra sign-in
- the API is running and returns `401 Unauthorized` on `GET /api/users`, which confirms the app started successfully and reached the post-migration runtime path
- WebSockets and Always On are enabled on the web app
- Always On is enabled on the API app

Remaining manual validation:

- sign in through the deployed web app in a browser
- confirm the redirected login returns to `https://stm-web-dev-weu-e5bd93.azurewebsites.net/signin-oidc`
- verify the web UI can load users, dashboard data, and tasks from the deployed API after authentication
