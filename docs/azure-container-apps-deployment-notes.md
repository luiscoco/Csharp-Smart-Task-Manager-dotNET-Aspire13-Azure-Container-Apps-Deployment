# Azure Container Apps Deployment Notes

## Current Deployment

This repository was deployed to Azure Container Apps on 2026-05-04 using Aspire 13 and Azure Developer CLI generated infrastructure.

Deployed Azure resources:

- Azure resource group: `rg-smarttaskmanager-aca-dev-weu`
- Azure Container Apps environment: `acaenvm32vher53gck6`
- Azure Container Registry: `acaenvacrm32vher53gck6`
- Log Analytics workspace: `acaenvlaw-m32vher53gck6`
- User-assigned managed identity: `aca_env_mi-m32vher53gck6`
- Public web Container App: `smarttaskmanager-web`
- Internal API Container App: `smarttaskmanager-api`

Runtime endpoints:

- Web FQDN: `https://smarttaskmanager-web.nicemoss-be74dfa1.westeurope.azurecontainerapps.io`
- API internal FQDN: `smarttaskmanager-api.internal.nicemoss-be74dfa1.westeurope.azurecontainerapps.io`

The existing App Service deployment was not changed. Local SQL data was not migrated during the ACA deployment.

## Runtime Shape

- `smarttaskmanager-web` is the only public Container App.
- `smarttaskmanager-api` has internal ingress only.
- `smarttaskmanager-web` calls the API through `SmartTaskManagerApi__BaseUrl`.
- `SmartTaskManagerApi__BaseUrl` currently points to the internal ACA API FQDN.
- Both apps run with `minReplicas: 1` and `maxReplicas: 1`.
- One replica is intentional for the first Blazor Server ACA deployment. Configure sticky sessions before scaling the web app above one replica.

## Secret Handling

ACA secret references:

- Web client secret: `azuread--clientsecret`
- SQL connection string: `connectionstrings--smarttaskmanager`

Application settings using secret refs:

- `smarttaskmanager-web`: `AzureAd__ClientSecret`
- `smarttaskmanager-api`: `ConnectionStrings__SmartTaskManager`

Do not commit `.azure/`. It is ignored because it contains local azd environment state and can contain deployment values.

The web client secret used during the deployment was exposed in chat. Rotate it in Microsoft Entra and update the `smarttaskmanager-web` ACA secret before treating the deployment as production-ready.

## Entra Redirect URIs

The Web app registration has these ACA redirect URIs:

- `https://smarttaskmanager-web.nicemoss-be74dfa1.westeurope.azurecontainerapps.io/signin-oidc`
- `https://smarttaskmanager-web.nicemoss-be74dfa1.westeurope.azurecontainerapps.io/signout-callback-oidc`

Keep the existing App Service and localhost redirect URIs only if those environments remain active.

## Deployment Issue Encountered

`azd provision` succeeded.

`azd deploy` successfully pushed container images to ACR but failed while applying per-service Container App Bicep because secure parameters were sent as empty values:

- API failure: `ContainerAppSecretInvalid` for `connectionstrings--smarttaskmanager`
- Web failure: `ContainerAppSecretInvalid` for `azuread--clientsecret`

The workaround was:

1. Use `azd deploy <service>` to build and push each image.
2. Get the newest image tag from ACR.
3. Deploy each generated Container App module directly with `az deployment group create`.
4. Pass the SQL connection string and web client secret as secure Bicep parameters.
5. Verify the resulting Container Apps use ACA secret refs.

## Direct Bicep Fallback

Use this fallback only after `azd provision` succeeds and `azd deploy` fails with blank secure parameters.

Prerequisites:

- ACR endpoint from the azd environment outputs.
- ACA environment ID and default domain from azd environment outputs.
- ACR pull managed identity ID from azd environment outputs.
- Latest API and web image tags from ACR.
- Secret values supplied from local environment variables, not pasted into the command history.

API module:

```powershell
az deployment group create `
  --resource-group <resource-group> `
  --name manual-smarttaskmanager-api `
  --template-file .\infra\smarttaskmanager-api\smarttaskmanager-api-containerapp.module.bicep `
  --parameters `
    aca_env_outputs_azure_container_apps_environment_default_domain=<aca-default-domain> `
    aca_env_outputs_azure_container_apps_environment_id=<aca-env-id> `
    smarttaskmanager_api_containerimage=<api-image> `
    smarttaskmanager_api_containerport=8080 `
    smarttaskmanagersqlconnectionstring_value=$env:SMARTTASKMANAGER_SQL_CONNECTION_STRING `
    apiazureadinstance_value="https://login.microsoftonline.com/" `
    apiazureadtenantid_value="<tenant-id>" `
    apiazureadclientid_value="<api-client-id>" `
    apiazureadaudience_value="<api-audience>" `
    apiauthorizationrequiredscope_value="Tasks.ReadWrite" `
    aca_env_outputs_azure_container_registry_endpoint=<acr-login-server> `
    aca_env_outputs_azure_container_registry_managed_identity_id=<acr-pull-mi-id>
```

Web module:

```powershell
az deployment group create `
  --resource-group <resource-group> `
  --name manual-smarttaskmanager-web `
  --template-file .\infra\smarttaskmanager-web\smarttaskmanager-web-containerapp.module.bicep `
  --parameters `
    aca_env_outputs_azure_container_apps_environment_default_domain=<aca-default-domain> `
    aca_env_outputs_azure_container_apps_environment_id=<aca-env-id> `
    smarttaskmanager_web_containerimage=<web-image> `
    smarttaskmanager_web_containerport=8080 `
    webazureadinstance_value="https://login.microsoftonline.com/" `
    webazureadtenantid_value="<tenant-id>" `
    webazureadclientid_value="<web-client-id>" `
    webazureadclientsecret_value=$env:SMARTTASKMANAGER_WEB_CLIENT_SECRET `
    webazureadcallbackpath_value="/signin-oidc" `
    webazureadsignedoutcallbackpath_value="/signout-callback-oidc" `
    smarttaskmanagerapiaudience_value="<api-audience>" `
    smarttaskmanagerapiscopes_value="<api-scope>" `
    aca_env_outputs_azure_container_registry_endpoint=<acr-login-server> `
    aca_env_outputs_azure_container_registry_managed_identity_id=<acr-pull-mi-id>
```

## Post-Deploy Validation

Validate:

- `smarttaskmanager-web` has `external: true`.
- `smarttaskmanager-api` has `external: false`.
- Both apps have one active revision with one running replica.
- Web GET redirects to Microsoft Entra and uses the ACA `signin-oidc` URI.
- Web app can sign in and load dashboard, users, tasks, and task details.
- API logs show no startup failures.
- API SQL connectivity works through the deployed app.
- ACA settings use secret refs rather than literal secret values.

## Rollback

Do not delete the existing App Service deployment.

Rollback options:

- Keep ACA resources and point users back to the App Service URL.
- Disable traffic or stop ACA apps if cost reduction is needed.
- Delete only `rg-smarttaskmanager-aca-dev-weu` after explicit approval if the ACA deployment is no longer needed.
