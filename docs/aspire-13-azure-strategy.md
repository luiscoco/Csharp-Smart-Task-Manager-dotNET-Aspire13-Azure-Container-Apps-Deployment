# Aspire 13 Azure Strategy

## Purpose

This document records the Azure deployment strategy for this Aspire 13.x migrated repository.

It does not deploy anything, create Azure resources, or replace the current deployment path. It compares the Azure options after Aspire migration and recommends how this repo should be handled in Azure.

## Repository Facts Verified

The repository currently contains:

- `src/SmartTaskManager.AppHost` using `Aspire.AppHost.Sdk/13.0.0`
- `src/SmartTaskManager.ServiceDefaults`
- `src/SmartTaskManager.Web`
- `src/SmartTaskManager.Api`
- a manual Azure App Service deployment path in `docs/azure-app-service-commands.md` and `scripts/deploy-appservice.ps1`
- a Bicep template for App Service in `infra/appservice.bicep`

The current Azure shape is documented as:

- one shared `B1` App Service plan
- one web app for `SmartTaskManager.Web`
- one web app for `SmartTaskManager.Api`
- external Azure SQL Database
- Microsoft Entra app registrations and App Service settings

The source confirms:

- `SmartTaskManager.Web` uses Microsoft Entra OpenID Connect through `Microsoft.Identity.Web`
- `SmartTaskManager.Web` requires `AzureAd:ClientSecret` from user secrets or environment variables
- `SmartTaskManager.Web` calls the API through `SmartTaskManagerApi:BaseUrl`
- `SmartTaskManager.Api` validates Microsoft Entra bearer tokens
- `SmartTaskManager.Api` requires `ConnectionStrings:SmartTaskManager`
- `SmartTaskManager.Api` applies EF Core database initialization at startup
- Aspire currently models only the web and API projects and keeps the database external

Relevant local docs:

- `docs/azure-app-service-deployment-options.md`
- `docs/azure-app-service-commands.md`
- `docs/azure-app-service-entra-and-settings.md`
- `docs/aspire-13-migration-plan.md`
- `docs/aspire-13-auth-endpoints-and-config.md`
- `docs/aspire-13-azure-deployment-impact.md`

## Official Aspire App Service Facts

Microsoft's current Aspire App Service guidance matters because this decision affects cost and deployment risk.

Important official facts:

- Aspire Azure App Service integration is currently preview and subject to change.
- The integration is provided by `Aspire.Hosting.Azure.AppService`.
- `AddAzureAppServiceEnvironment` represents App Service hosting infrastructure for Aspire publishing.
- By default, `AddAzureAppServiceEnvironment` provisions a Premium `P0V3` Linux App Service plan, Azure Container Registry Basic SKU, user-assigned managed identity, and ACR role assignments.
- The Azure App Service quickstart also shows a managed Aspire Dashboard resource, container build, ACR push, and container deployment to App Service during `azd up`.
- App Service communication for Aspire apps currently requires external HTTP endpoints; App Service does not currently manage traffic between apps through internal endpoints the way Azure Container Apps does.
- `WithExternalHttpEndpoints()` is therefore required for backend services that other Aspire app services call and frontend apps that users access directly.
- App Service plan SKU and tier can be customized through Aspire provisioning APIs, and existing App Service plans can be referenced with `AsExisting`, but this adds AppHost provisioning/configuration complexity.

Official references:

- [Aspire Azure App Service integration (Preview)](https://learn.microsoft.com/en-us/dotnet/aspire/azure/azure-app-service-integration)
- [Quickstart: Deploy an Aspire app to Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnet-aspire)
- [Configure an Aspire app for Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnet-aspire)
- [Local Azure provisioning in Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/azure/local-provisioning)

## Evaluation Criteria

The decision is based on:

- cost impact
- operational complexity
- Microsoft Entra compatibility
- Azure SQL compatibility
- compatibility with separate web/API apps
- effect on the current low-cost App Service design
- preview risk
- rollback complexity

## Option 1: Keep Aspire Local Only And Keep Manual Azure App Service Deployment

### Description

Use Aspire for local orchestration, service defaults, local dashboard, and local developer workflow only.

Keep Azure deployment as:

- `SmartTaskManager.Web` deployed directly to Azure App Service
- `SmartTaskManager.Api` deployed directly to Azure App Service
- one shared low-cost `B1` App Service plan
- existing Azure SQL
- existing Microsoft Entra app registrations and redirect URI model
- existing App Service app settings and secret handling

Do not deploy:

- `SmartTaskManager.AppHost`
- `SmartTaskManager.ServiceDefaults` as a standalone app
- Aspire-managed Azure App Service environment resources

### Cost Impact

This option preserves the current low-cost design.

The current repo already documents and uses a single shared `B1` App Service plan for both apps. That is cheaper and simpler than the default Aspire App Service provisioning shape, which starts from a Premium Linux App Service plan plus supporting resources unless customized.

### Operational Complexity

This is the lowest operational complexity option because the existing deployment docs and scripts remain the source of truth.

Operational responsibilities stay familiar:

- publish web/API separately
- configure web/API App Service settings separately
- keep Entra redirect URIs aligned with the web app host
- keep the API SQL connection string in API App Service settings

### Microsoft Entra Compatibility

Strong fit.

This option preserves the current hostnames, callback paths, client IDs, scopes, and secret placement model:

- `AzureAd__ClientSecret` remains an App Service setting on the web app only
- API bearer-token validation remains unchanged
- web redirect URI remains tied to the web app public hostname
- API audience and delegated scope settings remain unchanged

### Azure SQL Compatibility

Strong fit.

The API continues to use `ConnectionStrings__SmartTaskManager` from Azure App Service settings. The current Azure SQL Database remains external to Aspire, which matches the existing AppHost decision to keep the database external for stage 1.

### Separate Web/API Compatibility

Strong fit.

The current Azure shape already uses two App Service web apps. That directly matches the current solution structure and deployment docs.

### Effect On Current Low-Cost App Service Design

Positive.

This option protects the existing low-cost `B1` plan strategy and avoids accidental movement to a Premium App Service plan or container-registry-backed deployment flow.

### Preview Risk

Low.

Aspire remains a local development dependency for orchestration and service defaults. The production-like deployment path does not depend on the preview Aspire App Service integration.

### Rollback Complexity

Low.

If Aspire local orchestration causes issues, rollback can remove AppHost and ServiceDefaults references without changing Azure resources.

## Option 2: Adopt Aspire Azure App Service Integration For Deployment

### Description

Move deployment into the Aspire AppHost by adding the Azure App Service integration and publishing web/API as App Service websites through Aspire/AZD.

Likely AppHost concepts would include:

- `builder.AddAzureAppServiceEnvironment("app-service-env")`
- `.WithExternalHttpEndpoints()` on both web and API projects
- `.PublishAsAzureAppServiceWebsite(...)` on web and API
- App Service app settings configured through Azure provisioning APIs
- possible `AsExisting(...)` use if trying to reuse the current App Service plan

### Cost Impact

Higher risk for this repo.

The official default provisions a Premium `P0V3` Linux App Service plan, Azure Container Registry Basic SKU, managed identity, and role assignments. The quickstart also shows a managed Aspire Dashboard resource and container publishing.

That default is materially different from the current low-cost `B1` setup. It may be customizable, but the burden shifts to proving that Aspire-generated infrastructure exactly preserves the low-cost design before it can be recommended.

### Operational Complexity

Higher.

This option changes deployment ownership from direct App Service scripts/Bicep to AppHost-driven Azure provisioning. The team would need to manage:

- AZD environment state
- Aspire Azure provisioning configuration
- generated Bicep behavior
- ACR/image lifecycle
- managed identity and role assignment behavior
- dashboard deployment and access model
- AppHost code as infrastructure definition
- differences between Aspire local run behavior and Aspire publish behavior

### Microsoft Entra Compatibility

Possible, but not automatic.

The web and API can still use Microsoft Entra, but the deployment must explicitly preserve:

- web app public hostname and redirect URI registration
- `AzureAd__ClientSecret` placement
- API audience
- delegated scope settings
- `SmartTaskManagerApi__BaseUrl`

If Aspire-generated app names or hostnames differ from the current deployed web/API names, Entra redirect URIs must be updated before browser sign-in works.

### Azure SQL Compatibility

Possible, but must be explicit.

The current AppHost does not model Azure SQL. The safest Aspire deployment experiment would keep Azure SQL external and inject `ConnectionStrings__SmartTaskManager` into the API App Service settings.

Modeling Azure SQL inside Aspire is not recommended for this repo right now because it would expand scope beyond the current low-cost, already-working deployment path.

### Separate Web/API Compatibility

Possible.

The official quickstart shows separate API and frontend App Service web apps. However, App Service currently requires external HTTP endpoints for service-to-service communication, so the API must be publicly reachable unless additional networking/access restrictions are deliberately added later.

That is compatible with the current repo, because the API already exists as a separate public App Service app protected by Microsoft Entra bearer tokens.

### Effect On Current Low-Cost App Service Design

Negative unless heavily customized.

The default Aspire App Service deployment shape is not the current low-cost `B1` Windows App Service design. Preserving cost would require explicit SKU/tier customization or `AsExisting(...)` against the current plan, then careful review of the generated resources.

### Preview Risk

High.

The integration is preview. For this repository, the current manual deployment path is already documented and has deployed successfully. Moving deployment to a preview integration would introduce deployment risk without solving a current blocker.

### Rollback Complexity

Medium to high.

Rollback depends on whether Aspire created new resources or modified existing ones.

If Aspire is tested in a separate resource group, rollback can be as simple as `azd down` for that environment. If Aspire is pointed at existing resources, rollback becomes more complex because AppHost-generated infrastructure and app settings may overlap with the current manual deployment path.

## Option 3: Hybrid Approach

### Description

Use Aspire locally now, keep manual App Service deployment as the production-like Azure path, and evaluate Aspire App Service deployment later in a separate branch and isolated Azure environment.

Hybrid means:

- Aspire is adopted for local orchestration first
- current manual App Service deployment remains the supported Azure path
- Azure SQL remains external
- Entra remains configured against the deployed web hostname
- Aspire App Service integration is treated as a preview evaluation only

If Aspire App Service is evaluated later, require:

- a separate resource group
- no changes to the current deployed resource group unless explicitly approved
- no reuse of production-like Entra redirect URIs until hostnames are known
- no reuse of secrets without a deliberate secret-handling plan
- explicit cost review of generated resources before leaving anything running
- explicit rollback plan before `azd up`

### Cost Impact

Best balance.

The current low-cost deployment stays intact, while an Aspire App Service proof-of-concept can be cost-bounded and deleted after review.

### Operational Complexity

Moderate only during the evaluation.

Day-to-day operations remain simple because the manual path stays authoritative. The Aspire App Service path remains an experimental branch/documented option until it proves it can preserve cost and configuration.

### Microsoft Entra Compatibility

Good if evaluation hostnames are isolated.

The current app registrations can remain untouched for the active deployment. A later evaluation can add temporary redirect URIs only when needed.

### Azure SQL Compatibility

Good.

The evaluation can keep using an external SQL connection string. A safer variant would use a separate dev database or a copy, not the current active database.

### Separate Web/API Compatibility

Good.

The hybrid path keeps the current separate web/API App Service apps. A later Aspire evaluation can also model separate projects, but must use external endpoints for App Service.

### Effect On Current Low-Cost App Service Design

Positive.

The existing `B1` design remains protected. Any Aspire deployment must prove it can match or deliberately replace that design before adoption.

### Preview Risk

Contained.

Preview risk is isolated to a proof-of-concept, not the primary deployment path.

### Rollback Complexity

Low for the active deployment.

If the preview evaluation uses a separate resource group, rollback is resource-group or `azd down` cleanup. The active manual deployment is unaffected.

## Comparison Summary

| Criterion | Option 1: Local Aspire + Manual Azure | Option 2: Aspire App Service Deployment | Option 3: Hybrid |
| --- | --- | --- | --- |
| Cost impact | Preserves current low-cost `B1` design | Likely higher by default; requires customization | Preserves current cost; allows bounded experiment |
| Operational complexity | Low | High | Low day-to-day, moderate during evaluation |
| Entra compatibility | Strong | Possible but must rework hostnames/settings carefully | Strong for current path; controlled for evaluation |
| Azure SQL compatibility | Strong | Possible if kept external | Strong |
| Separate web/API apps | Strong | Possible; external endpoints required | Strong |
| Low-cost App Service effect | Helps preserve it | Harms it unless customized | Preserves it |
| Preview risk | Low | High | Contained |
| Rollback complexity | Low | Medium/high | Low if isolated |

## Recommendation

Recommended Azure strategy:

1. Keep Aspire for local orchestration first.
2. Keep the current manual Azure App Service + Azure SQL deployment path for now.
3. Treat Aspire Azure App Service deployment as a later preview evaluation, not the recommended deployment path.

This repo should not move Azure deployment itself into Aspire right now.

The strongest reason is cost and risk alignment: the current Azure deployment is already optimized around a low-cost shared `B1` App Service plan with two separate apps and external Azure SQL. The default Aspire App Service deployment path is preview and provisions additional infrastructure unless deliberately customized.

## What Aspire Should Own Now

Aspire should own:

- local multi-project orchestration
- local dashboard and telemetry defaults
- local web/API dependency ordering
- stage-1 local service composition

Aspire should not own yet:

- Azure App Service resource creation
- Azure App Service deployment
- Azure SQL provisioning
- Entra app registration changes
- production-like app settings or secrets

## Conditions For Reconsidering Aspire App Service Deployment Later

Revisit Aspire App Service deployment only if one of these becomes true:

- the Aspire App Service integration becomes generally available or the preview risk is explicitly accepted
- the team wants container-based App Service deployment and ACR is acceptable
- the generated infrastructure can be reviewed and customized to preserve the intended App Service SKU and resource count
- the current manual deployment path becomes too costly to maintain
- the team wants AppHost code to become the primary infrastructure definition

Before adoption, a later proof-of-concept must prove:

- exact App Service plan SKU and tier
- exact generated resources and costs
- App Service app names and hostnames
- Entra redirect URI changes
- API public endpoint behavior
- Azure SQL connection-string handling
- dashboard enablement or disablement
- rollback procedure

## Final Decision

Adopt Aspire only for local orchestration first.

Keep the current manual Azure App Service deployment path as the recommended Azure path for this repository.

Do not adopt Aspire Azure App Service deployment yet, except as a later isolated preview proof-of-concept.
