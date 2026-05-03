# Aspire 13 Azure Container Apps Deployment Prompts

## Purpose

This document contains copy-ready prompts and a deployment flow explanation for deploying the current Aspire 13 SmartTaskManager application to Azure Container Apps.

This is a planning and execution guide only. Creating this document does not deploy anything, create Azure resources, migrate data, or change application code.

## Guardrails

Use these guardrails in every prompt until the actual deployment step is explicitly approved:

- Do not run `az`, `azd`, Azure PowerShell, or any Azure command.
- Do not provision, update, or delete Azure resources.
- Do not migrate local SQL data.
- Do not commit or print real secrets.
- Do not modify application code unless the specific prompt says to prepare local deployment changes.
- Keep Azure SQL external unless a later prompt explicitly changes that decision.
- Keep Microsoft Entra authentication in place.

## Current Repository Facts

Verified project shape:

- Solution: `SmartTaskManager.sln`
- Aspire AppHost: `src/SmartTaskManager.AppHost/SmartTaskManager.AppHost.csproj`
- AppHost SDK: `Aspire.AppHost.Sdk/13.0.0`
- Service defaults: `src/SmartTaskManager.ServiceDefaults`
- Web app: `src/SmartTaskManager.Web`
- API app: `src/SmartTaskManager.Api`
- AppHost resource names:
  - `smarttaskmanager-api`
  - `smarttaskmanager-web`
- Current AppHost models only the web and API projects.
- The database is external and configured through `ConnectionStrings:SmartTaskManager`.

Important runtime behavior:

- `SmartTaskManager.Web` uses Microsoft Entra OpenID Connect through `Microsoft.Identity.Web`.
- `SmartTaskManager.Web` requires `AzureAd:ClientSecret` from user secrets or environment variables.
- `SmartTaskManager.Web` calls the API through `SmartTaskManagerApi:BaseUrl`.
- `SmartTaskManager.Api` validates Microsoft Entra bearer tokens.
- `SmartTaskManager.Api` requires `ConnectionStrings:SmartTaskManager`.
- `SmartTaskManager.Api` applies database initialization during startup.
- The web app uses interactive server components, so Azure Container Apps needs WebSocket-compatible ingress behavior and either one replica initially or sticky sessions before scaling out.

## Deployment Flow Explanation

### 1. Readiness Review

Start with a read-only review. The goal is to confirm what must change before deployment without touching code or Azure.

The review should answer:

- whether the current AppHost is sufficient for ACA deployment
- which Aspire ACA package or hosting integration is needed
- how the web and API should be exposed
- where secrets belong
- how Entra redirect URIs will change after ACA creates the web FQDN
- whether the API can be internal-only
- whether Blazor Server session affinity or replica limits are needed

### 2. Prepare Local Deployment Files

After the readiness review, prepare the minimal local files needed for Aspire/AZD deployment to ACA.

Likely changes include:

- adding the Aspire Azure Container Apps hosting integration to the AppHost project
- adding an ACA environment in `AppHost.cs`, commonly through `AddAzureContainerAppEnvironment("aca-env")`
- preserving `smarttaskmanager-api` and `smarttaskmanager-web` as resource names
- adding or updating `azure.yaml` so AZD can identify the AppHost
- optionally customizing generated container app resources if ingress, secrets, replica counts, or session affinity require it

This step should still avoid Azure commands. Local validation should be limited to commands such as `dotnet build`.

### 3. Decide Ingress Shape

Recommended initial ACA shape:

- `smarttaskmanager-web`: public HTTPS ingress
- `smarttaskmanager-api`: internal ingress if the web container can reach it reliably

This is a good fit because the Blazor web app calls the API from the server-side web process. Browser clients should not need direct access to the API. The API should still keep Microsoft Entra bearer-token protection even if ingress is internal.

If generated Aspire configuration cannot produce a stable internal API URL for `SmartTaskManagerApi__BaseUrl`, expose the API publicly temporarily, keep Entra bearer-token protection, and document the reason.

### 4. Plan Secrets And Settings

Secrets should be injected through ACA/AZD secret handling, not committed to source.

Web app settings:

- `AzureAd__Instance`
- `AzureAd__TenantId`
- `AzureAd__ClientId`
- `AzureAd__ClientSecret`
- `AzureAd__CallbackPath`
- `AzureAd__SignedOutCallbackPath`
- `SmartTaskManagerApi__BaseUrl`
- `SmartTaskManagerApi__Audience`
- `SmartTaskManagerApi__Scopes`

API app settings:

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

### 5. Generate And Review Infrastructure Before Deployment

Before running any deployment, generate or inspect the infrastructure that Aspire/AZD will create.

Review for:

- Azure Container Apps environment
- Azure Container Registry
- Log Analytics workspace
- managed identity
- role assignments
- Aspire Dashboard behavior
- public ingress only on the web app
- internal API ingress if feasible
- secret references instead of inline secret values
- web replica count or sticky-session settings

Stop before cloud deployment and ask for approval.

### 6. Deploy Only After Explicit Approval

The actual deployment step should run only after showing the exact Azure commands and receiving explicit approval.

Recommended deployment constraints:

- use a new isolated Azure resource group
- use a new isolated AZD environment name
- do not touch the existing App Service deployment
- do not migrate SQL data
- use the existing Azure SQL connection string only as a secret
- expose only the web app publicly unless the API must be public
- update Entra redirect URIs only after the web ACA FQDN is known

### 7. Update Microsoft Entra Redirect URIs

After ACA creates the public web FQDN, update the web app registration with:

- `https://<web-aca-fqdn>/signin-oidc`
- `https://<web-aca-fqdn>/signout-callback-oidc`

The API app registration should keep:

- Application ID URI: `api://3bede5d9-a947-4d25-a3c1-54df15d5ed60`
- Delegated scope: `Tasks.ReadWrite`

### 8. Verify Deployment

Verification should confirm:

- the web ACA app loads over HTTPS
- the web app redirects to Microsoft Entra sign-in
- sign-in callback works
- sign-out callback works
- the API starts successfully
- protected API endpoints return `401 Unauthorized` without a token
- authenticated web UI can load dashboard, users, tasks, and task details
- API database initialization succeeds against Azure SQL
- logs show no missing `AzureAd`, `SmartTaskManagerApi`, or `ConnectionStrings__SmartTaskManager` settings

### 9. Rollback

For an isolated ACA proof of concept, rollback should target only the new ACA resources.

Do not delete the existing App Service or Azure SQL resources. Do not delete a shared resource group unless it was created only for this ACA deployment.

## Prompt 1: Read-Only ACA Readiness Review

```text
Review this repository for deploying the existing Aspire 13 app to Azure Container Apps.

Hard constraints:
- Do not modify files.
- Do not run az, azd, Azure PowerShell, or any Azure CLI command.
- Do not migrate data or change Azure resources.

Repo facts to verify:
- Solution: SmartTaskManager.sln
- Aspire AppHost: src/SmartTaskManager.AppHost/SmartTaskManager.AppHost.csproj
- AppHost SDK: Aspire.AppHost.Sdk/13.0.0
- Projects modeled in AppHost:
  - smarttaskmanager-api -> SmartTaskManager.Api
  - smarttaskmanager-web -> SmartTaskManager.Web
- Keep Azure SQL external.
- Keep Microsoft Entra auth.

Produce an Azure Container Apps readiness report covering:
- required AppHost changes
- required azd/azure.yaml setup
- required ACA secrets and environment variables
- Entra redirect URI changes for the final ACA web FQDN
- whether the API can stay internal to ACA
- Blazor Server considerations: WebSockets, sticky sessions, or one replica
- risks, cost-impacting resources, and rollback plan
```

## Prompt 2: Prepare Code/Config Changes Only

```text
Prepare the minimal code/config changes needed to make this existing Aspire 13 app deployable to Azure Container Apps.

Hard constraints:
- Do not run az, azd, Azure PowerShell, or any Azure command.
- Do not provision or deploy anything.
- Do not migrate SQL data.
- Do not commit secrets.

Implementation intent:
- Add the Aspire Azure Container Apps hosting integration to SmartTaskManager.AppHost, using the Aspire 13.x package line.
- Update AppHost to register an ACA environment, likely with AddAzureContainerAppEnvironment("aca-env").
- Preserve current Aspire resource names:
  - smarttaskmanager-api
  - smarttaskmanager-web
- Prefer exposing only smarttaskmanager-web publicly.
- Keep smarttaskmanager-api internal unless the generated SmartTaskManagerApi__BaseUrl cannot be made reachable from the web container.
- Keep Azure SQL external through ConnectionStrings__SmartTaskManager.
- Keep Entra settings as environment variables/secrets, not source code.
- For SmartTaskManager.Web, account for Blazor Server hosting by using one replica initially or configuring sticky sessions if multiple replicas are enabled.

After editing, run only local validation commands such as dotnet build. Report exact files changed and the deployment commands that would be run later, but do not run them.
```

## Prompt 3: Secrets And Entra Mapping

```text
Create the Azure Container Apps secret and environment-variable plan for this repository.

Hard constraints:
- Do not run Azure commands.
- Do not print or store real secret values.
- Do not migrate anything.

Required web settings:
- AzureAd__Instance=https://login.microsoftonline.com/
- AzureAd__TenantId=e099cebd-5eea-41a3-88db-bcb9a9cba83e
- AzureAd__ClientId=ffdda8ba-1389-4fa9-bba5-b06d14ef55e5
- AzureAd__ClientSecret=<secret>
- AzureAd__CallbackPath=/signin-oidc
- AzureAd__SignedOutCallbackPath=/signout-callback-oidc
- SmartTaskManagerApi__BaseUrl=<generated API URL, preferably internal ACA URL reachable from web>
- SmartTaskManagerApi__Audience=api://3bede5d9-a947-4d25-a3c1-54df15d5ed60
- SmartTaskManagerApi__Scopes=api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite

Required API settings:
- ConnectionStrings__SmartTaskManager=<Azure SQL connection string secret>
- AzureAd__Instance=https://login.microsoftonline.com/
- AzureAd__TenantId=e099cebd-5eea-41a3-88db-bcb9a9cba83e
- AzureAd__ClientId=3bede5d9-a947-4d25-a3c1-54df15d5ed60
- AzureAd__Audience=api://3bede5d9-a947-4d25-a3c1-54df15d5ed60
- Authorization__RequiredScope=Tasks.ReadWrite
- Database__EnableEfLogging=false
- Database__EnableDetailedErrors=false
- Database__EnableSensitiveDataLogging=false
- Seeding__EnableSampleData=false

Also produce the Entra redirect URI update plan for:
- https://<web-aca-fqdn>/signin-oidc
- https://<web-aca-fqdn>/signout-callback-oidc
```

## Prompt 4: Generate And Review Infra, No Deploy

```text
Generate and review Azure Container Apps deployment artifacts for this Aspire 13 repository, but do not deploy.

Hard constraints:
- Do not run azd up.
- Do not run azd provision.
- Do not run azd deploy.
- Do not run az commands that create, update, or delete Azure resources.
- Do not migrate data.

Allowed only if needed:
- local manifest generation
- local azd initialization or infra generation
- local file edits required for review

Review generated artifacts for:
- Azure Container Apps environment
- Azure Container Registry
- Log Analytics workspace
- managed identity and role assignments
- Aspire Dashboard
- public ingress only for smarttaskmanager-web
- internal/private ingress for smarttaskmanager-api if feasible
- secret references for web client secret and SQL connection string
- generated SmartTaskManagerApi__BaseUrl behavior
- replica count or sticky-session configuration for Blazor Server

Stop before any cloud operation and summarize the exact Azure commands that would be needed later.
```

## Prompt 4 Local Artifact Review Results

Local artifacts generated for review:

- `src/SmartTaskManager.AppHost/artifacts/aca-review/manifest/aspire-manifest.json`
- `src/SmartTaskManager.AppHost/artifacts/aca-review/manifest/aca-env.module.bicep`
- `src/SmartTaskManager.AppHost/artifacts/aca-review/manifest/smarttaskmanager-api-containerapp.module.bicep`
- `src/SmartTaskManager.AppHost/artifacts/aca-review/manifest/smarttaskmanager-web-containerapp.module.bicep`

No Azure deployment command was run. `azd` was not available on this machine during review, so full `azd infra gen` output was not generated. The local Aspire manifest publisher did generate reviewable ACA Bicep modules.

Generated resource review:

- Azure Container Apps environment is generated in `aca-env.module.bicep`.
- Azure Container Registry is generated with Basic SKU.
- Log Analytics workspace is generated with `PerGB2018` billing.
- User-assigned managed identity is generated for Container Apps image pull access.
- `AcrPull` role assignment is generated from the managed identity to ACR.
- Aspire Dashboard is generated as an ACA environment .NET component named `aspire-dashboard`.
- `smarttaskmanager-web` has public ingress with `external: true`.
- `smarttaskmanager-api` has internal ingress with `external: false`.
- SQL connection string is a secure parameter and ACA secret reference for the API.
- Web Entra client secret is a secure parameter and ACA secret reference for the web app.
- Both container apps use `activeRevisionsMode: Single`.
- Both container apps are set to `minReplicas: 1` and `maxReplicas: 1`.

`SmartTaskManagerApi__BaseUrl` review:

- The generated web app setting currently points to `https://smarttaskmanager-api.internal.<aca-default-domain>`.
- The generated API ingress uses `transport: 'http'`.
- Before deployment, confirm whether ACA internal HTTPS works as generated for this service-to-service call.
- If validation or generated infra review shows the internal HTTPS URL is not reachable from the web container, change the AppHost setting to use the API HTTP endpoint instead.

Blazor Server review:

- The one-replica configuration is acceptable for the first ACA deployment because server-side Blazor circuits do not need cross-replica affinity.
- Sticky sessions are not configured in the generated Bicep.
- If `smarttaskmanager-web` is later scaled beyond one replica, configure session affinity before increasing `maxReplicas`.

Configuration review:

- The generated Bicep only externalizes the SQL connection string and web client secret because those were modeled as publish-time Aspire parameters.
- Non-secret Entra settings remain available from checked-in appsettings files unless the AppHost is extended to emit them as ACA environment variables.
- For stricter environment-driven production config, add explicit AppHost environment variables for the non-secret settings listed in Prompt 3.

Review risks:

- The generated Container Apps modules use preview API versions.
- Aspire Dashboard adds a resource to review for cost, exposure, and operational need.
- Log Analytics can become cost-impacting if verbose logs are retained or traffic increases.
- `minReplicas: 1` avoids Blazor scale-out complexity but prevents scale-to-zero savings.

## Prompt 5: Actual Deployment Later

```text
Deploy the existing Aspire 13 SmartTaskManager app to Azure Container Apps.

Before running any Azure command:
- Show me the exact azd/az commands you intend to run.
- Explain what each command will create or change.
- Wait for my explicit approval.

Deployment constraints:
- Use a new isolated Azure resource group and azd environment.
- Do not touch the existing App Service deployment.
- Do not migrate local SQL data.
- Use the existing Azure SQL Database connection string as an ACA secret.
- Expose only the web container app publicly unless there is a documented reason to expose the API.
- Keep the API protected by Microsoft Entra bearer tokens.
- Update Entra redirect URIs only after the web ACA FQDN is known.
- Verify web sign-in and web-to-api calls after deployment.
```

## Prompt 6: Verification And Rollback

```text
Verify the Azure Container Apps deployment of SmartTaskManager.

Check:
- smarttaskmanager-web is reachable publicly over HTTPS.
- web redirects to Microsoft Entra sign-in.
- Entra callback and signed-out callback work.
- smarttaskmanager-api starts successfully.
- protected API endpoints return 401 without a token.
- authenticated web UI can load dashboard, users, tasks, and task details.
- API can connect to Azure SQL.
- logs show no missing configuration for AzureAd, SmartTaskManagerApi, or ConnectionStrings__SmartTaskManager.

Do not delete or change Azure resources unless I explicitly ask. If rollback is needed, propose a cleanup plan first and wait for approval.
```

## Non-Executed Command Flow For Later Approval

These are examples of the kinds of commands a later deployment prompt may propose. Do not run them until the actual deployment task is approved.

Local-only review command, if `azd` is installed:

```powershell
azd infra gen
```

Cloud deployment command flow after approval:

```powershell
azd auth login
azd env new <azd-environment-name>
azd up
```

Staged alternative after approval:

```powershell
azd auth login
azd env new <azd-environment-name>
azd provision
azd deploy
```

Expected high-level behavior:

1. `azd infra gen` generates local infrastructure artifacts for review only.
2. `azd auth login` authenticates the developer CLI to Azure.
3. `azd env new <azd-environment-name>` creates/selects isolated local AZD environment metadata.
4. `azd up` provisions Azure resources and deploys the containerized Aspire resources.
5. The staged alternative splits provisioning and deployment into `azd provision` and `azd deploy`.
6. Aspire/AZD generates an app model from the AppHost, builds container images, pushes them to Azure Container Registry, and updates Azure Container Apps to run those images.
7. After the web FQDN is known, Microsoft Entra redirect URIs must be updated.
8. After Entra is aligned, perform authenticated browser validation.

## References

- [Deploy an Aspire project to Azure Container Apps using Azure Developer CLI](https://learn.microsoft.com/en-us/dotnet/aspire/deployment/azure/aca-deployment-azd-in-depth)
- [Configure Azure Container Apps environments in Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/azure/configure-aca-environments)
- [Session Affinity in Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/sticky-sessions)
