# Aspire 13 Migration Plan

## Purpose

This document records the executed incremental migration of the existing `SmartTaskManager` solution to Aspire `13.x` without rewriting the application architecture.

The migration was executed on `2026-04-30` with the locally installed stable Aspire `13.0.0` toolchain.

## Executed Migration Snapshot

Execution choices used for this repository:

- Aspire version line used: `13.0.x`
- actual local Aspire CLI/AppHost package version used: `13.0.0`
- migration method used: manual AppHost + ServiceDefaults creation
- new projects created:
  - `src/SmartTaskManager.AppHost`
  - `src/SmartTaskManager.ServiceDefaults`
- existing projects modified:
  - `src/SmartTaskManager.Web`
  - `src/SmartTaskManager.Api`
  - `SmartTaskManager.sln`

Actual first-stage code changes:

- added `SmartTaskManager.AppHost` and `SmartTaskManager.ServiceDefaults`
- added ServiceDefaults project references to the web and API projects
- added `builder.AddServiceDefaults()` to the web and API startup
- added `app.MapDefaultEndpoints()` to the web and API startup
- modeled the Web and API in AppHost
- used the existing `https` launch profiles in AppHost
- kept `SmartTaskManagerApi__BaseUrl` and injected it from the AppHost API endpoint
- kept the database external

## Validated Repository Facts

The repository currently contains:

- solution: `SmartTaskManager.sln`
- projects in the solution:
  - `src/SmartTaskManager.Domain`
  - `src/SmartTaskManager.Application`
  - `src/SmartTaskManager.Infrastructure`
  - `src/SmartTaskManager.Api`
  - `src/SmartTaskManager.Web`

Validated technical facts from source:

- `SmartTaskManager.Web` targets `net10.0`
- `SmartTaskManager.Api` targets `net10.0`
- `SmartTaskManager.Web` uses Microsoft Entra OpenID Connect through `Microsoft.Identity.Web`
- `SmartTaskManager.Web` validates that `AzureAd:ClientSecret` must come from user secrets or environment variables
- `SmartTaskManager.Web` currently calls the API through `SmartTaskManagerApi:BaseUrl`
- `SmartTaskManager.Api` uses Microsoft Entra JWT bearer auth
- `SmartTaskManager.Api` uses `ConnectionStrings:SmartTaskManager`
- `SmartTaskManager.Api` auto-applies EF Core migrations on startup

Validated local launch URLs from `launchSettings.json`:

- `SmartTaskManager.Web`
  - HTTPS: `https://localhost:7036`
  - HTTP: `http://localhost:5269`
- `SmartTaskManager.Api`
  - HTTPS: `https://localhost:7081`
  - HTTP: `http://localhost:5081`

Validated auth-sensitive local callback URLs:

- sign-in callback: `https://localhost:7036/signin-oidc`
- sign-out callback: `https://localhost:7036/signout-callback-oidc`

Repository notes that affect migration planning:

- no `global.json` was found
- no `Directory.Packages.props` was found
- there is a `tempTest\tempTest.csproj` file in the repo, but it is not part of `SmartTaskManager.sln`

## Official Aspire 13 Guidance Snapshot

Based on current official Aspire guidance:

- Aspire `13.x` requires `.NET 10 SDK or later`
- `aspire init` is a stable command for adding Aspire support to an existing solution
- `aspire update` exists, but it is for updating an existing Aspire solution and is not the right first tool for initial adoption here
- Aspire Service Defaults adds:
  - OpenTelemetry defaults
  - health endpoints
  - service discovery
  - `HttpClient` defaults with resilience and service discovery
- Aspire launch profile behavior can affect service project ports and proxies, so stable browser-visible local URLs must be planned deliberately

Version recommendation:

- use the stable Aspire `13.x` line
- prefer the `stable` channel
- do not use `staging`, `daily`, or Aspire `9.x`

Actual execution note:

- the machine already had Aspire CLI `13.0.0` installed
- to keep the migration low-risk, the migration was executed on that `13.0.0` line instead of combining the migration with a toolchain upgrade

## Decision 1: `aspire init` vs Manual Project Creation

### Option A: `aspire init`

Pros:

- officially supported for existing solutions
- fastest way to bootstrap an AppHost
- automatically adds required Aspire support

Cons for this repository:

- lower control over project placement and naming
- more automated edits in one step than this repo needs
- higher chance of creating layout drift from the existing `src/` structure
- higher risk of touching more files than necessary in a repo with Entra callback constraints and existing deployment docs
- requires extra caution because the repo contains a non-solution `tempTest` project

### Option B: manual template creation

Pros:

- exact control over project names and paths
- easier to keep new projects under `src/`
- easier to review, test, and roll back step by step
- better fit for preserving auth, ports, and current deployment conventions
- lower surprise factor than broad automatic scaffolding

Cons:

- more manual steps
- slightly slower setup

### Recommendation

Recommended first migration path: manual project creation.

Reason:

- this repo has sensitive local Microsoft Entra redirect behavior
- it already has a stable layering and deployment story
- the lowest-risk approach is explicit, incremental, and reversible

`aspire init` remains a valid alternative, but it is not the recommended first path for this specific codebase.

## Decision 2: Where To Place The New Aspire Projects

Recommended locations:

- `src/SmartTaskManager.AppHost`
- `src/SmartTaskManager.ServiceDefaults`

Why:

- it preserves the current solution convention where all first-class projects live under `src/`
- it keeps the solution tree predictable
- it avoids mixing orchestration projects at the solution root while the rest of the codebase lives elsewhere

## Decision 3: Minimal Orchestration vs Deeper Service Discovery Refactor

### Option A: Minimal Aspire orchestration

Characteristics:

- add AppHost
- add ServiceDefaults
- enroll only `SmartTaskManager.Web` and `SmartTaskManager.Api`
- keep the existing `SmartTaskManagerApi__BaseUrl` configuration contract initially
- let AppHost provide the runtime API target to the web app
- keep the database external

Pros:

- smallest code delta
- preserves current typed options and validation flow
- avoids rewriting the existing typed `HttpClient` setup in the first step
- easiest rollback

Cons:

- not a fully idiomatic Aspire service discovery refactor yet
- leaves one compatibility layer in place intentionally

### Option B: Deeper Aspire service discovery refactor

Characteristics:

- remove or reduce reliance on `SmartTaskManagerApi:BaseUrl`
- refactor the web app to depend directly on Aspire-style service discovery naming
- possibly rework option validation and API client setup

Pros:

- cleaner long-term Aspire shape
- less legacy configuration over time

Cons:

- larger code change
- more moving parts during the first migration
- higher chance of auth or endpoint regressions

### Recommendation

Recommended first step: minimal Aspire orchestration.

Exact recommendation:

- keep `SmartTaskManagerApi__BaseUrl` in stage 1
- have the AppHost inject the runtime API target into the web app
- also model a reference from the web app to the API in the AppHost so a later service discovery refactor remains straightforward

Why this is lower risk:

- the web app already validates `SmartTaskManagerApiOptions`
- the current typed client setup assumes a URL-shaped value
- a full service discovery rewrite is unnecessary to gain AppHost orchestration, telemetry defaults, and local multi-project startup

Actual execution result:

- this recommendation was implemented as designed
- the AppHost injects `SmartTaskManagerApi__BaseUrl` from `api.GetEndpoint("https")`
- because the external AppHost proxy preserved `https://localhost:7081`, the existing web configuration contract remains valid during local Aspire runs

## Decision 4: Stable Local HTTPS Endpoints For Microsoft Entra

This is the most important migration constraint.

Current required local web URL:

- `https://localhost:7036`

Current local Entra callbacks:

- `https://localhost:7036/signin-oidc`
- `https://localhost:7036/signout-callback-oidc`

Migration rule:

- do not allow Aspire adoption to casually change the browser-visible local web HTTPS port

Recommended approach:

- keep matching `https` launch profile names across:
  - AppHost
  - `SmartTaskManager.Web`
  - `SmartTaskManager.Api`
- validate after migration that the web app remains browser-visible on `https://localhost:7036`
- if the default Aspire proxy behavior changes the visible web URL, explicitly configure the web project endpoint in AppHost so the visible HTTPS port remains stable

Important nuance from official Aspire launch-profile guidance:

- Aspire reads launch profiles to derive endpoints
- Aspire may proxy service processes internally
- therefore the migration must validate the browser-visible port, not only the process binding

Actual execution result:

- browser-visible web HTTPS URL remained `https://localhost:7036`
- browser-visible API HTTPS URL remained `https://localhost:7081`
- AppHost proxy also preserved:
  - web HTTP `http://localhost:5269`
  - API HTTP `http://localhost:5081`
- the child processes ran behind the proxy on dynamic internal ports:
  - web: `https://localhost:64638`, `http://localhost:64639`
  - API: `https://localhost:64634`, `http://localhost:64635`

## Decision 5: Keep The Database External

Recommended first-step database approach:

- keep the SQL database external to Aspire
- continue using `ConnectionStrings:SmartTaskManager`
- continue using the current local SQL Server / Azure SQL connection-string model

Do not introduce in stage 1:

- SQL Server containers
- SQL resource hosting in Aspire
- Redis or other new backing infrastructure

Why:

- the API already has a stable external database contract
- startup migrations already depend on that connection string
- introducing a database container would increase scope without being necessary for AppHost adoption

## Decision 6: Service Defaults Changes Needed

The planned execution step should add Aspire Service Defaults only to:

- `SmartTaskManager.Web`
- `SmartTaskManager.Api`

No Service Defaults changes are needed for:

- `SmartTaskManager.Domain`
- `SmartTaskManager.Application`
- `SmartTaskManager.Infrastructure`

Expected code-level changes later during execution:

- add a project reference from Web to `SmartTaskManager.ServiceDefaults`
- add a project reference from API to `SmartTaskManager.ServiceDefaults`
- call `builder.AddServiceDefaults()` early in both startup pipelines
- call `app.MapDefaultEndpoints()` in both web-facing apps

Why this is clean:

- only executable service projects need the hosting-related defaults
- the current layered class libraries remain unchanged

## Recommended Migration Path

Recommended exact path for this repo:

1. Manually create `src/SmartTaskManager.AppHost`.
2. Manually create `src/SmartTaskManager.ServiceDefaults`.
3. Add both projects to `SmartTaskManager.sln`.
4. Reference ServiceDefaults from `SmartTaskManager.Web` and `SmartTaskManager.Api`.
5. Add Aspire Service Defaults to the web and API startup code.
6. Model only the web app and API in AppHost.
7. Keep the database external and keep `ConnectionStrings:SmartTaskManager`.
8. Keep `SmartTaskManagerApi__BaseUrl` initially and let AppHost provide the runtime API target.
9. Preserve the browser-visible local web URL at `https://localhost:7036`.
10. Validate local orchestration first.
11. Keep the current Azure App Service + Azure SQL deployment path unchanged for the short term.

Execution status:

- all steps above were completed for the local repository migration

## Risks

- Microsoft Entra redirect breakage if the local web HTTPS URL changes
- accidental broad edits if `aspire init` is used without tight review
- `SmartTaskManagerApi__BaseUrl` regression if it is removed too early
- API startup migration failure if the external connection string is missing or redirected to the wrong database
- stale local-host documentation causing confusion during validation
- Aspire proxy behavior changing the visible local endpoint unless explicitly validated

Observed execution risks:

- the generated ServiceDefaults project introduced current OpenTelemetry `NU1902` vulnerability warnings
- the pre-existing `System.Security.Cryptography.Xml` `9.0.0` vulnerability warnings remain in `SmartTaskManager.Infrastructure`
- direct HTTPS probing from this shell was unreliable because of the local Windows TLS stack in this session, so runtime validation relied on Aspire/DCP logs plus HTTP redirect checks

## Validation Results

Validated successfully:

- solution build succeeded after migration
- AppHost started successfully
- AppHost dashboard started on `https://localhost:17184`
- DCP marked these resources `Ready`:
  - `smarttaskmanager-web-http`
  - `smarttaskmanager-web-https`
  - `smarttaskmanager-api-http`
  - `smarttaskmanager-api-https`
- the web app started successfully with no client-secret validation failure
- the API started successfully with no connection-string regression
- the API reached the database and completed startup initialization
- EF Core startup logs showed:
  - database connectivity succeeded
  - `__EFMigrationsHistory` was checked
  - no migrations were pending
  - sample data seeding completed
- the preserved external API URL stayed `https://localhost:7081`, which matches the existing stage-1 web configuration contract

Remaining manual validation step:

- open `https://localhost:7036`
- sign in with the expected Microsoft Entra user
- confirm the authenticated UI can call the API through the preserved `https://localhost:7081` proxy path

## Rollback Strategy

The recommended path is intentionally reversible.

Rollback scope after the later execution prompt would be:

1. remove `SmartTaskManager.AppHost` from the solution
2. remove `SmartTaskManager.ServiceDefaults` from the solution
3. remove ServiceDefaults project references from the web and API projects
4. remove `AddServiceDefaults` and `MapDefaultEndpoints` calls from the web and API startup code
5. retain the existing app architecture, launch settings, app settings, Azure deployment scripts, and database setup

Because there is no CPM file and no current Aspire structure in the repo, rollback remains straightforward.

## Short Conclusion

Best first migration:

- manual Aspire `13.x` adoption
- AppHost and ServiceDefaults under `src/`
- minimal orchestration only
- external database retained
- current web-to-API config contract retained initially
- current Azure deployment path retained

This delivers the main Aspire benefits with the lowest risk to auth, ports, and deployment.
