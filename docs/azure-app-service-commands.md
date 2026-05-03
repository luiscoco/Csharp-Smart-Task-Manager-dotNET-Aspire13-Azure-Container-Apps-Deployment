# Azure App Service Commands

## Purpose

This document provides non-executed Azure CLI and PowerShell-friendly commands for deploying:

- `SmartTaskManager.Web`
- `SmartTaskManager.Api`

These commands assume:

- both apps are deployed separately
- the API database connection string is supplied later through App Service settings
- the Entra client secret is supplied later through App Service settings on the web app only

The commands below are examples and placeholders. Do not run them blindly without replacing names and secrets.

## Prereqs

### Local Tools

- Azure CLI
- .NET 10 SDK
- PowerShell

### Azure Login

```powershell
az login
az account show
```

If needed, select a subscription:

```powershell
$SubscriptionId = "<subscription-id>"
az account set --subscription $SubscriptionId
```

### Naming Variables

Use a consistent naming block before any commands:

```powershell
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-dev"

$FreePlan = "asp-smarttaskmanager-free"
$PaidPlan = "asp-smarttaskmanager-b1"

$WebAppName = "smarttaskmanager-web-dev"
$ApiAppName = "smarttaskmanager-api-dev"

$WebPublishDir = "C:\tmp\smarttaskmanager-web-publish"
$ApiPublishDir = "C:\tmp\smarttaskmanager-api-publish"
$WebZip = "C:\tmp\smarttaskmanager-web.zip"
$ApiZip = "C:\tmp\smarttaskmanager-api.zip"
```

### Publish The Applications

Publish both apps explicitly in `Release` mode:

```powershell
dotnet publish .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj -c Release -o $WebPublishDir
dotnet publish .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj -c Release -o $ApiPublishDir
```

Package the publish output:

```powershell
if (Test-Path $WebZip) { Remove-Item $WebZip -Force }
if (Test-Path $ApiZip) { Remove-Item $ApiZip -Force }

Compress-Archive -Path "$WebPublishDir\*" -DestinationPath $WebZip
Compress-Archive -Path "$ApiPublishDir\*" -DestinationPath $ApiZip
```

## Free / Dev-Test Profile

### Create The Resource Group

```powershell
az group create --name $ResourceGroup --location $Location
```

### Create One Shared `F1` App Service Plan

Use one shared plan for both apps:

```powershell
az appservice plan create `
  --resource-group $ResourceGroup `
  --name $FreePlan `
  --location $Location `
  --sku F1
```

### Create The Web Apps

```powershell
az webapp create `
  --resource-group $ResourceGroup `
  --plan $FreePlan `
  --name $WebAppName

az webapp create `
  --resource-group $ResourceGroup `
  --plan $FreePlan `
  --name $ApiAppName
```

### Configure The Free Profile Runtime Behavior

Enable WebSockets on the interactive web app:

```powershell
az webapp config set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --web-sockets-enabled true
```

Keep client affinity enabled on the web app:

```powershell
az resource update `
  --resource-group $ResourceGroup `
  --resource-type "Microsoft.Web/sites" `
  --name $WebAppName `
  --set properties.clientAffinityEnabled=true
```

Note:

- do not rely on Always On behavior in the Free profile
- expect idle unload and warm-up latency

## Paid `B1` Profile

### Create The Resource Group

```powershell
az group create --name $ResourceGroup --location $Location
```

### Create One Shared `B1` App Service Plan

```powershell
az appservice plan create `
  --resource-group $ResourceGroup `
  --name $PaidPlan `
  --location $Location `
  --sku B1
```

### Create The Web Apps

```powershell
az webapp create `
  --resource-group $ResourceGroup `
  --plan $PaidPlan `
  --name $WebAppName

az webapp create `
  --resource-group $ResourceGroup `
  --plan $PaidPlan `
  --name $ApiAppName
```

### Configure The Paid Profile Runtime Behavior

Web front end:

```powershell
az webapp config set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --web-sockets-enabled true `
  --always-on true

az resource update `
  --resource-group $ResourceGroup `
  --resource-type "Microsoft.Web/sites" `
  --name $WebAppName `
  --set properties.clientAffinityEnabled=true
```

API:

```powershell
az webapp config set `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --always-on true
```

## App Settings

### Web App Settings

Replace placeholders before running:

```powershell
az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --settings `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__TenantId="e099cebd-5eea-41a3-88db-bcb9a9cba83e" `
    AzureAd__ClientId="ffdda8ba-1389-4fa9-bba5-b06d14ef55e5" `
    AzureAd__ClientSecret="<set-later-in-secret-rotation-step>" `
    AzureAd__CallbackPath="/signin-oidc" `
    AzureAd__SignedOutCallbackPath="/signout-callback-oidc" `
    SmartTaskManagerApi__BaseUrl="https://$ApiAppName.azurewebsites.net/" `
    SmartTaskManagerApi__Audience="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    SmartTaskManagerApi__Scopes="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite"
```

### API App Settings

Replace placeholders before running:

```powershell
az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --settings `
    ConnectionStrings__SmartTaskManager="Server=tcp:<sql-server>.database.windows.net,1433;Initial Catalog=<sql-database>;Persist Security Info=False;User ID=<sql-admin>;Password=<sql-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__TenantId="e099cebd-5eea-41a3-88db-bcb9a9cba83e" `
    AzureAd__ClientId="3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    AzureAd__Audience="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    Authorization__RequiredScope="Tasks.ReadWrite"
```

Optional production toggles for the API:

```powershell
az webapp config appsettings set `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --settings `
    Database__EnableEfLogging="false" `
    Database__EnableDetailedErrors="false" `
    Database__EnableSensitiveDataLogging="false" `
    Seeding__EnableSampleData="false"
```

## Deployment Commands

Deploy the published zip packages:

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

If you prefer Kudu zip push deployment:

```powershell
az webapp deployment source config-zip `
  --resource-group $ResourceGroup `
  --name $WebAppName `
  --src $WebZip

az webapp deployment source config-zip `
  --resource-group $ResourceGroup `
  --name $ApiAppName `
  --src $ApiZip
```

## Verification Commands

### Check App URLs

```powershell
az webapp show --resource-group $ResourceGroup --name $WebAppName --query defaultHostName -o tsv
az webapp show --resource-group $ResourceGroup --name $ApiAppName --query defaultHostName -o tsv
```

### Inspect Applied App Settings

```powershell
az webapp config appsettings list --resource-group $ResourceGroup --name $WebAppName
az webapp config appsettings list --resource-group $ResourceGroup --name $ApiAppName
```

### Inspect General App Configuration

```powershell
az webapp config show --resource-group $ResourceGroup --name $WebAppName
az webapp config show --resource-group $ResourceGroup --name $ApiAppName
```

### Tail Logs

```powershell
az webapp log tail --resource-group $ResourceGroup --name $WebAppName
az webapp log tail --resource-group $ResourceGroup --name $ApiAppName
```

### Restart If Needed

```powershell
az webapp restart --resource-group $ResourceGroup --name $WebAppName
az webapp restart --resource-group $ResourceGroup --name $ApiAppName
```

## Cleanup Commands

Delete the entire resource group:

```powershell
az group delete --name $ResourceGroup --yes --no-wait
```

Delete only the web apps:

```powershell
az webapp delete --resource-group $ResourceGroup --name $WebAppName
az webapp delete --resource-group $ResourceGroup --name $ApiAppName
```

Delete only the App Service Plan:

```powershell
az appservice plan delete --resource-group $ResourceGroup --name $PaidPlan --yes
```

For the Free profile, substitute `$FreePlan` as needed.

## Execution Snapshot 2026-04-30

These are the actual values used for the successful paid deployment:

```powershell
$SubscriptionId = "e5bd93f3-dcd9-4833-a589-82e16245997c"
$Location = "westeurope"
$ResourceGroup = "rg-smarttaskmanager-data-dev-weu"

$PaidPlan = "asp-stm-dev-weu-b1"
$WebAppName = "stm-web-dev-weu-e5bd93"
$ApiAppName = "stm-api-dev-weu-e5bd93"

$WebPublishDir = "C:\tmp\smarttaskmanager-web-publish"
$ApiPublishDir = "C:\tmp\smarttaskmanager-api-publish"
$WebZip = "C:\tmp\smarttaskmanager-web.zip"
$ApiZip = "C:\tmp\smarttaskmanager-api.zip"
```

The deployed SQL connection string target was:

```text
Server=tcp:sql-stm-dev-weu-01.database.windows.net,1433;Initial Catalog=SmartTaskManagerDb;Persist Security Info=False;User ID=stmsqladmin;Password=<redacted>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

The actual deployment flow that completed was:

```powershell
dotnet publish .\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj -c Release -o C:\tmp\smarttaskmanager-web-publish
dotnet publish .\src\SmartTaskManager.Api\SmartTaskManager.Api.csproj -c Release -o C:\tmp\smarttaskmanager-api-publish

Compress-Archive -Path "C:\tmp\smarttaskmanager-web-publish\*" -DestinationPath C:\tmp\smarttaskmanager-web.zip
Compress-Archive -Path "C:\tmp\smarttaskmanager-api-publish\*" -DestinationPath C:\tmp\smarttaskmanager-api.zip

az group create --name rg-smarttaskmanager-data-dev-weu --location westeurope

az appservice plan create `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name asp-stm-dev-weu-b1 `
  --location westeurope `
  --sku B1

az webapp create `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --plan asp-stm-dev-weu-b1 `
  --name stm-web-dev-weu-e5bd93 `
  --runtime "dotnet:10"

az webapp create `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --plan asp-stm-dev-weu-b1 `
  --name stm-api-dev-weu-e5bd93 `
  --runtime "dotnet:10"

az resource update `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --resource-type "Microsoft.Web/sites" `
  --name stm-web-dev-weu-e5bd93 `
  --set properties.httpsOnly=true properties.clientAffinityEnabled=true

az resource update `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --resource-type "Microsoft.Web/sites" `
  --name stm-api-dev-weu-e5bd93 `
  --set properties.httpsOnly=true properties.clientAffinityEnabled=false

az webapp config set `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name stm-web-dev-weu-e5bd93 `
  --web-sockets-enabled true `
  --always-on true

az webapp config set `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name stm-api-dev-weu-e5bd93 `
  --always-on true

az webapp config appsettings set `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name stm-web-dev-weu-e5bd93 `
  --settings `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__TenantId="e099cebd-5eea-41a3-88db-bcb9a9cba83e" `
    AzureAd__ClientId="ffdda8ba-1389-4fa9-bba5-b06d14ef55e5" `
    AzureAd__ClientSecret="<copied-from-local-user-secrets>" `
    AzureAd__CallbackPath="/signin-oidc" `
    AzureAd__SignedOutCallbackPath="/signout-callback-oidc" `
    SmartTaskManagerApi__BaseUrl="https://stm-api-dev-weu-e5bd93.azurewebsites.net/" `
    SmartTaskManagerApi__Audience="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    SmartTaskManagerApi__Scopes="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite"

az webapp config appsettings set `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name stm-api-dev-weu-e5bd93 `
  --settings `
    ConnectionStrings__SmartTaskManager="Server=tcp:sql-stm-dev-weu-01.database.windows.net,1433;Initial Catalog=SmartTaskManagerDb;Persist Security Info=False;User ID=stmsqladmin;Password=<redacted>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" `
    AzureAd__Instance="https://login.microsoftonline.com/" `
    AzureAd__TenantId="e099cebd-5eea-41a3-88db-bcb9a9cba83e" `
    AzureAd__ClientId="3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    AzureAd__Audience="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60" `
    Authorization__RequiredScope="Tasks.ReadWrite" `
    Database__EnableEfLogging="false" `
    Database__EnableDetailedErrors="false" `
    Database__EnableSensitiveDataLogging="false" `
    Seeding__EnableSampleData="false"

az webapp deploy `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name stm-web-dev-weu-e5bd93 `
  --src-path C:\tmp\smarttaskmanager-web.zip `
  --type zip

az webapp deploy `
  --resource-group rg-smarttaskmanager-data-dev-weu `
  --name stm-api-dev-weu-e5bd93 `
  --src-path C:\tmp\smarttaskmanager-api.zip `
  --type zip
```

Validation commands used:

```powershell
Invoke-WebRequest -Uri "https://stm-web-dev-weu-e5bd93.azurewebsites.net/"
Invoke-WebRequest -Uri "https://stm-api-dev-weu-e5bd93.azurewebsites.net/api/users"
az webapp config appsettings list --resource-group rg-smarttaskmanager-data-dev-weu --name stm-web-dev-weu-e5bd93
az webapp config appsettings list --resource-group rg-smarttaskmanager-data-dev-weu --name stm-api-dev-weu-e5bd93
az webapp config show --resource-group rg-smarttaskmanager-data-dev-weu --name stm-web-dev-weu-e5bd93
az webapp config show --resource-group rg-smarttaskmanager-data-dev-weu --name stm-api-dev-weu-e5bd93
```

## Rollback And Cleanup Commands

This App Service deployment reused the existing resource group that already contains Azure SQL resources. Because of that, deleting the whole resource group will also delete the Azure SQL logical server and database.

Safest rollback:

```powershell
az webapp delete --resource-group rg-smarttaskmanager-data-dev-weu --name stm-web-dev-weu-e5bd93
az webapp delete --resource-group rg-smarttaskmanager-data-dev-weu --name stm-api-dev-weu-e5bd93
az appservice plan delete --resource-group rg-smarttaskmanager-data-dev-weu --name asp-stm-dev-weu-b1 --yes
```

If you also want to revert the Entra redirect URIs back to localhost only:

```powershell
az ad app update `
  --id ffdda8ba-1389-4fa9-bba5-b06d14ef55e5 `
  --web-redirect-uris `
    "https://localhost:5001/signout-callback-oidc" `
    "https://localhost:5001/signin-oidc"
```

Use this only if you intentionally want to remove the deployed Azure callbacks:

```powershell
az ad app show --id ffdda8ba-1389-4fa9-bba5-b06d14ef55e5 --query "web.redirectUris"
```

Use full resource-group deletion only if you want to remove both App Service and the existing Azure SQL resources:

```powershell
az group delete --name rg-smarttaskmanager-data-dev-weu --yes --no-wait
```
