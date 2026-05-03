# Aspire 13 Azure Commands

## Purpose

This document provides non-executed commands and steps for the recommended Azure path after the Aspire 13 migration.

Do not run these commands as part of this document review. This file is a command reference only.

## Recommended Azure Path

Recommended path:

- use Aspire locally through `SmartTaskManager.AppHost`
- keep deploying `SmartTaskManager.Web` and `SmartTaskManager.Api` separately to Azure App Service
- keep one shared low-cost `B1` App Service plan
- keep Azure SQL external
- keep Microsoft Entra app registration and App Service setting management in the existing manual path

Primary existing references:

- `docs/azure-app-service-commands.md`
- `scripts/deploy-appservice.ps1`
- `infra/appservice.bicep`
- `docs/azure-app-service-entra-and-settings.md`

## Read-Only Local Checks

These commands inspect the local repo. They do not deploy.

```powershell
git status --short
rg --files -g "*.sln" -g "*.csproj" -g "appsettings*.json" -g "*.ps1" -g "*.bicep"
rg -n "Aspire.AppHost.Sdk|AddServiceDefaults|MapDefaultEndpoints|AddAzureAppServiceEnvironment|PublishAsAzureAppServiceWebsite" .\src .\docs .\scripts .\infra
```

Optional read-only precheck script, if approved later:

```powershell
.\scripts\aspire-13-azure-precheck.ps1
```

The precheck script is intended to inspect local files only. It must not create Azure resources.

## Local Aspire Commands

Use these only for local development validation. They do not deploy to Azure.

```powershell
dotnet build .\SmartTaskManager.sln
dotnet run --project .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj --launch-profile https
```

Alternative Aspire CLI form:

```powershell
aspire run --project .\src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj
```

Local validation targets:

```powershell
Invoke-WebRequest -Uri https://localhost:7036/ -UseBasicParsing
Invoke-WebRequest -Uri https://localhost:7081/health -UseBasicParsing
Invoke-WebRequest -Uri https://localhost:7081/alive -UseBasicParsing
```

Manual browser validation still matters because Microsoft Entra sign-in requires an interactive user session.

## Manual Azure App Service Path

This remains the recommended Azure deployment path.

### Set Variables

Replace placeholders before running in a future approved deployment task.

```powershell
$SubscriptionId = "<subscription-id>"
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-data-dev-weu"

$PlanName = "asp-stm-dev-weu-b1"
$WebAppName = "stm-web-dev-weu-e5bd93"
$ApiAppName = "stm-api-dev-weu-e5bd93"

$WebPublishDir = "C:\tmp\smarttaskmanager-web-publish"
$ApiPublishDir = "C:\tmp\smarttaskmanager-api-publish"
$WebZip = "C:\tmp\smarttaskmanager-web.zip"
$ApiZip = "C:\tmp\smarttaskmanager-api.zip"
```

### Select Azure Subscription

Non-executed reference:

```powershell
az login
az account set --subscription $SubscriptionId
az account show
```

### Publish The Web And API Projects Separately

Non-executed reference:

```powershell
dotnet publish .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj -c Release -o $WebPublishDir
dotnet publish .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj -c Release -o $ApiPublishDir
```

Package outputs:

```powershell
if (Test-Path $WebZip) { Remove-Item $WebZip -Force }
if (Test-Path $ApiZip) { Remove-Item $ApiZip -Force }

Compress-Archive -Path "$WebPublishDir\*" -DestinationPath $WebZip
Compress-Archive -Path "$ApiPublishDir\*" -DestinationPath $ApiZip
```

### Provision Or Confirm The Low-Cost App Service Shape

The current low-cost shape is one shared `B1` plan and two apps.

Non-executed reference:

```powershell
az group create --name $ResourceGroup --location $Location

az appservice plan create `
  --resource-group $ResourceGroup `
  --name $PlanName `
  --location $Location `
  --sku B1

az webapp create `
  --resource-group $ResourceGroup `
  --plan $PlanName `
  --name $WebAppName `
  --runtime "dotnet:10"

az webapp create `
  --resource-group $ResourceGroup `
  --plan $PlanName `
  --name $ApiAppName `
  --runtime "dotnet:10"
```

### Configure App Service Runtime Behavior

Non-executed reference:

```powershell
az resource update `
  --resource-group $ResourceGroup `
  --resource-type "Microsoft.Web/sites" `
  --name $WebAppName `
  --set properties.httpsOnly=true properties.clientAffinityEnabled=true

az resource update `
  --resource-group $ResourceGroup `
  --resource-type "Microsoft.Web/sites" `
  --name $ApiAppName `
  --set properties.httpsOnly=true properties.clientAffinityEnabled=false

az webapp config set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --web-sockets-enabled true `
  --always-on true

az webapp config set `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --always-on true
```

### Configure Web App Settings

Non-executed reference. Do not put the real client secret in source control.

```powershell
$WebClientSecret = "<read-from-approved-secret-source>"

az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --settings `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__TenantId="e099cebd-5eea-41a3-88db-bcb9a9cba83e" `
    AzureAd__ClientId="ffdda8ba-1389-4fa9-bba5-b06d14ef55e5" `
    AzureAd__ClientSecret="$WebClientSecret" `
    AzureAd__CallbackPath="/signin-oidc" `
    AzureAd__SignedOutCallbackPath="/signout-callback-oidc" `
    SmartTaskManagerApi__BaseUrl="https://$ApiAppName.azurewebsites.net/" `
    SmartTaskManagerApi__Audience="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    SmartTaskManagerApi__Scopes="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite"
```

### Configure API App Settings

Non-executed reference. Do not put the real SQL password in source control.

```powershell
$SqlConnectionString = "Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<sql-database>;Persist Security Info=False;User ID=<sql-admin>;Password=<sql-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --settings `
    ConnectionStrings__SmartTaskManager="$SqlConnectionString" `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__TenantId="e099cebd-5eea-41a3-88db-bcb9a9cba83e" `
    AzureAd__ClientId="3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    AzureAd__Audience="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    Authorization__RequiredScope="Tasks.ReadWrite" `
    Database__EnableEfLogging="false" `
    Database__EnableDetailedErrors="false" `
    Database__EnableSensitiveDataLogging="false" `
    Seeding__EnableSampleData="false"
```

### Deploy The Separate Zip Packages

Non-executed reference:

```powershell
az webapp deploy `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --src-path $WebZip `
  --type zip

az webapp deploy `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --src-path $ApiZip `
  --type zip
```

### Verify The Manual Deployment

Non-executed reference:

```powershell
az webapp show --resource-group $ResourceGroup --name $WebAppName --query defaultHostName -o tsv
az webapp show --resource-group $ResourceGroup --name $ApiAppName --query defaultHostName -o tsv

az webapp config appsettings list --resource-group $ResourceGroup --name $WebAppName
az webapp config appsettings list --resource-group $ResourceGroup --name $ApiAppName

Invoke-WebRequest -Uri "https://$WebAppName.azurewebsites.net/" -UseBasicParsing
Invoke-WebRequest -Uri "https://$ApiAppName.azurewebsites.net/api/users" -UseBasicParsing
```

Expected API anonymous smoke result:

- `401 Unauthorized` on protected API endpoints is acceptable because it proves the app started and the bearer-token guard is active.

Expected web smoke result:

- the web app redirects to Microsoft Entra sign-in.

Final validation:

- sign in interactively through the deployed web app
- confirm dashboard, users, tasks, and task details load from the deployed API

## Manual Path Rollback Commands

Use only when intentionally rolling back the App Service deployment. Be careful if the resource group also contains Azure SQL.

Safest App Service only rollback:

```powershell
az webapp delete --resource-group $ResourceGroup --name $WebAppName
az webapp delete --resource-group $ResourceGroup --name $ApiAppName
az appservice plan delete --resource-group $ResourceGroup --name $PlanName --yes
```

Do not delete the whole resource group unless you also intend to delete Azure SQL and every other resource in it.

## Preview And Higher-Risk Aspire App Service Commands

This section is not the recommended path.

Use only for a later isolated proof-of-concept after explicit approval. These commands can create Azure resources and increase cost.

### Preview Risk Notice

The Aspire Azure App Service integration is preview. The default resource shape can include:

- Premium `P0V3` Linux App Service plan
- Azure Container Registry Basic SKU
- user-assigned managed identity
- role assignments
- managed Aspire Dashboard resource
- container build and push flow

This is higher risk and likely higher cost than the current manual `B1` deployment unless customized and reviewed.

### Preview POC Local Code Changes

Non-executed reference commands:

```powershell
aspire add azure-appservice
```

Illustrative AppHost code only. Do not apply without a separate implementation prompt.

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var appServiceEnv = builder.AddAzureAppServiceEnvironment("app-service-env");

var api = builder.AddProject<Projects.SmartTaskManager_Api>("smarttaskmanager-api", launchProfileName: "https")
    .WithExternalHttpEndpoints()
    .PublishAsAzureAppServiceWebsite((infra, website) =>
    {
        // Configure API App Service settings here.
        // Keep ConnectionStrings__SmartTaskManager external and secret-backed.
    });

builder.AddProject<Projects.SmartTaskManager_Web>("smarttaskmanager-web", launchProfileName: "https")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WithEnvironment("SmartTaskManagerApi__BaseUrl", api.GetEndpoint("https"))
    .WaitFor(api)
    .PublishAsAzureAppServiceWebsite((infra, website) =>
    {
        // Configure web App Service settings here.
        // Keep AzureAd__ClientSecret external and secret-backed.
    });

builder.Build().Run();
```

### Preview POC Existing Plan Variant

If the goal is to test Aspire while preserving the current App Service plan, investigate `AsExisting(...)` instead of creating a new plan.

Illustrative code only:

```csharp
var existingAppServicePlanName = builder.AddParameter("existingAppServicePlanName");
var existingResourceGroup = builder.AddParameter("existingResourceGroup");

var appServiceEnv = builder.AddAzureAppServiceEnvironment("app-service-env")
    .AsExisting(existingAppServicePlanName, existingResourceGroup);
```

This still requires Aspire Azure provisioning configuration such as subscription, resource group, and location.

### Preview POC AZD Commands

Do not run unless an isolated proof-of-concept is explicitly approved.

```powershell
azd init
azd auth login
azd up
```

Cleanup for the isolated preview environment:

```powershell
azd down
```

Before any preview `azd up`, require:

- separate resource group
- separate AZD environment name
- documented expected resource list
- expected App Service SKU and tier
- Entra redirect URI plan
- secret-handling plan
- SQL database plan
- cost review
- rollback plan

## Recommended Command Path

For the next implementation task, keep using the manual App Service deployment path.

Do not add `Aspire.Hosting.Azure.AppService` or run `azd up` unless the task is explicitly a preview proof-of-concept.
