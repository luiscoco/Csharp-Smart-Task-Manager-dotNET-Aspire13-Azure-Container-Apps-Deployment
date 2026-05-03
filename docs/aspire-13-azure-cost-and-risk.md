# Aspire 13 Azure Cost And Risk

## Purpose

This document summarizes the likely cost and resource impact of moving Azure deployment into Aspire compared with the existing manual Azure App Service deployment.

It does not deploy anything and does not create Azure resources.

## Current Low-Cost Azure Shape

The current documented deployment is optimized for low cost:

- one shared `B1` App Service plan
- two Azure App Service web apps:
  - `SmartTaskManager.Web`
  - `SmartTaskManager.Api`
- external Azure SQL Database
- Microsoft Entra app registrations
- App Service application settings for runtime configuration and secrets

The existing `infra/appservice.bicep` is constrained to `B1`, `Basic`, capacity `1`.

The current `scripts/deploy-appservice.ps1` defaults to:

```powershell
[string]$Sku = "B1"
```

This shape keeps compute spend concentrated in one shared App Service plan while preserving separate web and API apps.

## Aspire App Service Default Resource Impact

Official Aspire App Service guidance shows a larger default resource shape.

By default, `AddAzureAppServiceEnvironment` can provision:

- Premium `P0V3` Linux App Service plan
- Azure Container Registry Basic SKU
- user-assigned managed identity
- role assignments for ACR access

The Azure App Service quickstart also shows:

- a managed Aspire Dashboard resource
- container image builds
- container pushes to Azure Container Registry
- container deployment to App Service

This is a meaningful change from the current manual `B1` zip-deploy style path.

Official references:

- [Aspire Azure App Service integration (Preview)](https://learn.microsoft.com/en-us/dotnet/aspire/azure/azure-app-service-integration)
- [Quickstart: Deploy an Aspire app to Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnet-aspire)
- [Configure an Aspire app for Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-language-dotnet-aspire)

## Cost Comparison

| Area | Current manual App Service path | Aspire App Service default path |
| --- | --- | --- |
| App Service plan | One shared `B1` Basic plan | Premium `P0V3` Linux plan by default unless customized |
| App instances | Two web apps on shared plan | Two App Service apps, usually container-backed |
| Container registry | Not required | ACR Basic SKU by default |
| Managed identity | Not required for current zip deploy | User-assigned identity by default for ACR access |
| Dashboard in Azure | Not required | Managed Aspire Dashboard shown in quickstart/default guidance |
| Deployment artifacts | Local publish output and zip packages | Container images built and pushed to ACR |
| Azure SQL | Existing external database | Still needed unless separately modeled |
| Entra | Existing app registrations/settings | Still needed, with possible redirect URI churn |

## Does Aspire Deployment Help The Low-Cost Goal?

No, not right now.

Moving deployment itself into Aspire currently harms the low-cost deployment goal for this repo unless the Aspire deployment is heavily customized and proven to preserve the intended resource shape.

Reasons:

- the official default App Service plan is Premium `P0V3`, not `B1`
- ACR adds another billable resource
- a managed dashboard may add another resource to govern and potentially pay for
- container build/push/deploy adds operational steps not present in the current zip-deploy flow
- preview integration behavior can change
- preserving the current resource shape would require extra AppHost infrastructure customization or `AsExisting(...)`

Aspire helps local development cost indirectly by improving local orchestration and diagnostics. It does not reduce Azure hosting cost when used as the deployment mechanism for this repo today.

## Risk Summary

| Risk | Current manual path | Aspire App Service deployment |
| --- | --- | --- |
| Preview dependency | None for Azure deployment | High; App Service integration is preview |
| Cost drift | Low; `B1` is explicit in docs/scripts/Bicep | Medium/high; default is Premium unless customized |
| Resource sprawl | Low | Medium; ACR, managed identity, dashboard, role assignments |
| Entra breakage | Low if hostnames unchanged | Medium; generated hostnames may require redirect changes |
| SQL misconfiguration | Low; current API setting model is explicit | Medium; must re-create API connection-string injection |
| Web/API communication | Known current public API app model | Must use external endpoints on App Service |
| Rollback | Simple if only web/API/plan are touched | Simple only if isolated; harder if existing resources are reused |
| Operational familiarity | High | Lower; AppHost/AZD/provisioning-driven deployment |

## Microsoft Entra Risk

The current manual path is stable because the deployed web hostname and callback paths are known:

- `/signin-oidc`
- `/signout-callback-oidc`

If Aspire creates new App Service names or hostnames, Microsoft Entra redirect URIs must be updated before sign-in works.

The API audience and delegated scope values must also remain aligned:

- `SmartTaskManagerApi__Audience`
- `SmartTaskManagerApi__Scopes`
- `AzureAd__Audience` on the API

This is manageable, but it is not a cost or reliability improvement over the current path.

## Azure SQL Risk

The current API requires:

```text
ConnectionStrings__SmartTaskManager
```

The API initializes the database on startup. If the Aspire deployment path fails to inject the connection string correctly, the API can fail during startup or migration initialization.

The safest Azure SQL strategy remains:

- keep Azure SQL external
- keep the connection string in API App Service settings
- do not model SQL provisioning in Aspire during the first Azure strategy phase

## Separate Web/API Risk

The repo is already designed and deployed as separate web and API apps.

Aspire App Service can model this, but official App Service guidance requires external HTTP endpoints for service-to-service communication. That means the API remains public-facing at the App Service endpoint unless additional restrictions are added.

This is acceptable for the current design because the API is protected by Microsoft Entra bearer tokens, but it does not improve isolation compared with the manual path.

## Rollback Risk

### Manual Path

Rollback is direct:

- delete or redeploy the web app
- delete or redeploy the API app
- keep or delete the shared App Service plan
- leave Azure SQL untouched unless intentionally rolling back data resources

The existing docs already warn not to delete the resource group if it contains Azure SQL resources that should be retained.

### Aspire App Service Path

Rollback depends on test isolation.

If Aspire is evaluated in a separate resource group:

- `azd down` or resource-group deletion can remove the preview environment
- current manual deployment remains unaffected

If Aspire points at existing resources:

- generated app settings, role assignments, app service configuration, and deployment artifacts may overlap with the current manual path
- cleanup requires careful resource-by-resource review

Therefore, any Aspire App Service proof-of-concept should use a separate resource group first.

## Cost Controls If Aspire App Service Is Evaluated Later

Before running any Aspire App Service deployment later, require:

- Azure Pricing Calculator estimate for the generated resource list
- explicit App Service plan SKU and tier decision
- decision on whether to disable the Aspire Dashboard in Azure
- ACR retention and cleanup plan
- separate resource group
- unique AZD environment name
- short-lived proof-of-concept window
- cleanup command recorded before deployment

For a cost-sensitive evaluation, prefer:

- `AsExisting(...)` against a known App Service plan only after isolated testing
- `ConfigureInfrastructure(...)` only after generated Bicep has been reviewed
- no production-like Entra redirect changes until hostnames are final

## Recommended Risk Position

The current low-cost deployment goal is helped by keeping Aspire local only and keeping the manual App Service deployment path.

The current low-cost deployment goal is harmed by moving deployment itself into Aspire right now.

## Final Cost Decision

Do not adopt Aspire Azure App Service deployment as the default Azure path at this time.

Keep:

- one shared `B1` App Service plan
- separate web/API apps
- external Azure SQL
- existing Microsoft Entra settings
- existing manual deployment docs and scripts

Use Aspire for local orchestration first, then reassess deployment integration only in a separate preview proof-of-concept if the added cost and preview risk become acceptable.
