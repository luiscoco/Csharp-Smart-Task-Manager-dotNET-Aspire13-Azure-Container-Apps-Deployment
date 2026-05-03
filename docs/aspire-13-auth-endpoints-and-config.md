# Aspire 13 Auth, Endpoints, And Config

## Purpose

This document records the auth-sensitive and endpoint-sensitive facts that the Aspire 13 migration must preserve.

## Current Validated Local URLs

From the current `launchSettings.json` files:

### `SmartTaskManager.Web`

- HTTPS: `https://localhost:7036`
- HTTP: `http://localhost:5269`

### `SmartTaskManager.Api`

- HTTPS: `https://localhost:7081`
- HTTP: `http://localhost:5081`

## Actual Aspire Stage 1 Runtime Result

After the migration:

- AppHost dashboard runs at `https://localhost:17184`
- AppHost preserves these browser-visible app URLs:
  - `SmartTaskManager.Web`
    - HTTPS: `https://localhost:7036`
    - HTTP: `http://localhost:5269`
  - `SmartTaskManager.Api`
    - HTTPS: `https://localhost:7081`
    - HTTP: `http://localhost:5081`

Internal child-process bindings moved behind the AppHost proxy:

- `SmartTaskManager.Web`
  - internal HTTPS: `https://localhost:64638`
  - internal HTTP: `http://localhost:64639`
- `SmartTaskManager.Api`
  - internal HTTPS: `https://localhost:64634`
  - internal HTTP: `http://localhost:64635`

## Current Web-to-API Configuration Contract

The web app currently uses:

- section: `SmartTaskManagerApi`
- key: `SmartTaskManagerApi:BaseUrl`
- default value: `https://localhost:7081/`

Other related web settings:

- `SmartTaskManagerApi:Audience`
- `SmartTaskManagerApi:Scopes`

This contract is already part of typed option validation in `SmartTaskManager.Web`.

## Current Microsoft Entra Callback Implications

The web app currently expects:

- callback path: `/signin-oidc`
- signed-out callback path: `/signout-callback-oidc`

With the current local HTTPS port, the effective full local callback URLs are:

- sign-in redirect: `https://localhost:7036/signin-oidc`
- sign-out callback: `https://localhost:7036/signout-callback-oidc`

These URLs must remain valid during local Aspire development unless the Microsoft Entra app registration is intentionally updated.

## Current Secret And Runtime-Sensitive Settings

### Must stay out of source control

- `AzureAd:ClientSecret`
- `ConnectionStrings:SmartTaskManager`
- any future AppHost parameter values that contain secrets

### Current approved secret sources

- `.NET user-secrets` for local web secret storage
- environment variables
- Azure App Service application settings for deployed apps

Do not introduce real secrets into:

- tracked `appsettings.json`
- tracked `appsettings.Development.json`
- tracked `appsettings.Production.json`
- AppHost source files

## Current API Runtime Contract

The API currently expects:

- `ConnectionStrings:SmartTaskManager`
- `AzureAd`
- `Authorization:RequiredScope`

The API auto-applies EF Core migrations on startup, so any Aspire run must preserve a valid external connection string source.

## Recommended Aspire Stage 1 Configuration Strategy

### Keep the current `BaseUrl` contract initially

Recommended first-step behavior:

- preserve `SmartTaskManagerApi__BaseUrl`
- have the AppHost inject the runtime API target into the web app
- do not remove the existing `SmartTaskManagerApiOptions` validation in the first migration

Why:

- it preserves the current typed configuration surface
- it minimizes code churn in the typed `HttpClient` setup
- it keeps the first migration focused on orchestration, telemetry defaults, and startup composition

Actual implementation:

- this stage-1 strategy was implemented
- AppHost injects `SmartTaskManagerApi__BaseUrl` from the API `https` endpoint
- because the preserved external API proxy stayed on `https://localhost:7081`, the existing default web configuration remains compatible during local Aspire runs

### Also model an AppHost reference to the API

The AppHost should still model the relationship between:

- `SmartTaskManager.Web`
- `SmartTaskManager.Api`

This preserves the route to a later deeper service discovery refactor without forcing it into stage 1.

## Recommended Endpoint Strategy Under Aspire

### For the web app

The browser-visible HTTPS URL should remain:

- `https://localhost:7036`

Reason:

- this avoids immediate Microsoft Entra redirect churn

### For the API

Preferred first-step browser-visible HTTPS URL:

- `https://localhost:7081`

If Aspire proxy behavior changes the visible API port, that is less sensitive than the web app port, but it must still be documented and validated.

Actual validation result:

- the browser-visible web URL remained on `https://localhost:7036`
- the browser-visible API URL remained on `https://localhost:7081`
- this kept the local Microsoft Entra callback URLs stable:
  - `https://localhost:7036/signin-oidc`
  - `https://localhost:7036/signout-callback-oidc`

## Important Aspire Service Discovery Compatibility Note

Aspire service discovery supports service-style URIs such as:

- `https://api`
- `https://_dashboard.api`

For this repo, stage 1 should not force a move to `https+http://...` URI formats through `SmartTaskManagerApi__BaseUrl`, because the current option model validates `BaseUrl` as a URL and the goal is to keep the first migration low-risk.

If a later deeper refactor is approved, the web app can be moved to fuller service-discovery-first client setup.

## Docs With Stale Localhost Values

Current repo documentation still contains stale localhost callback references in some Azure deployment docs:

- `docs/azure-app-service-commands.md`
- `docs/azure-app-service-entra-and-settings.md`

Those docs currently mention `https://localhost:5001/...` in some historical sections.

For Aspire planning and execution, the correct current local values are:

- `https://localhost:7036/signin-oidc`
- `https://localhost:7036/signout-callback-oidc`
- `https://localhost:7081/` for the API base URL

## Recommended Auth-Safe Execution Rules

1. Keep the web app on `https://localhost:7036` during the first Aspire migration if at all possible.
2. Keep `AzureAd:ClientSecret` outside tracked files.
3. Keep the API database external and preserve `ConnectionStrings:SmartTaskManager`.
4. Keep `SmartTaskManagerApi__BaseUrl` initially and let AppHost provide the runtime value.
5. If Aspire changes the visible web port by default, explicitly override the AppHost endpoint behavior rather than updating Entra casually.

## Validation Notes

Validated automatically:

- the web app started under Aspire with no `AzureAd:ClientSecret` validation failure
- the API started under Aspire with no external connection-string regression
- the API proxy remained reachable on the preserved external port contract

Still requiring a manual browser check:

- open `https://localhost:7036`
- complete Microsoft Entra sign-in
- verify the authenticated web UI can call the API through the preserved `https://localhost:7081` proxy route

## Short Conclusion

The safest first Aspire migration for this repo is:

- preserve current web and API local HTTPS URLs as closely as possible
- preserve the current web-to-API config contract
- preserve the current secret-handling model
- preserve the external database connection model
