param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$PlanName,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [Parameter(Mandatory = $true)]
    [string]$ApiAppName,

    [Parameter(Mandatory = $true)]
    [string]$SqlConnectionString,

    [Parameter(Mandatory = $true)]
    [string]$WebClientSecret,

    [string]$Sku = "B1"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$webProject = Join-Path $repoRoot "src\SmartTaskManager.Web\SmartTaskManager.Web.csproj"
$apiProject = Join-Path $repoRoot "src\SmartTaskManager.Api\SmartTaskManager.Api.csproj"

$publishRoot = "C:\tmp\smarttaskmanager"
$webPublishDir = Join-Path $publishRoot "web"
$apiPublishDir = Join-Path $publishRoot "api"
$webZip = Join-Path $publishRoot "smarttaskmanager-web.zip"
$apiZip = Join-Path $publishRoot "smarttaskmanager-api.zip"

Write-Host "Creating resource group and App Service plan..."
az group create --name $ResourceGroup --location $Location | Out-Null
az appservice plan create --resource-group $ResourceGroup --name $PlanName --location $Location --sku $Sku | Out-Null

Write-Host "Creating Web Apps..."
az webapp create --resource-group $ResourceGroup --plan $PlanName --name $WebAppName | Out-Null
az webapp create --resource-group $ResourceGroup --plan $PlanName --name $ApiAppName | Out-Null

Write-Host "Applying App Service configuration..."
az webapp config set --resource-group $ResourceGroup --name $WebAppName --web-sockets-enabled true --always-on true | Out-Null
az resource update --resource-group $ResourceGroup --resource-type "Microsoft.Web/sites" --name $WebAppName --set properties.clientAffinityEnabled=true | Out-Null
az webapp config set --resource-group $ResourceGroup --name $ApiAppName --always-on true | Out-Null

Write-Host "Applying application settings..."
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
    SmartTaskManagerApi__Scopes="api://3bede5d9-a947-4d25-a3c1-54df15d5ed60/Tasks.ReadWrite" | Out-Null

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
    Seeding__EnableSampleData="false" | Out-Null

Write-Host "Publishing applications..."
New-Item -ItemType Directory -Force -Path $webPublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $apiPublishDir | Out-Null

dotnet publish $webProject -c Release -o $webPublishDir
dotnet publish $apiProject -c Release -o $apiPublishDir

if (Test-Path $webZip) { Remove-Item $webZip -Force }
if (Test-Path $apiZip) { Remove-Item $apiZip -Force }

Compress-Archive -Path "$webPublishDir\*" -DestinationPath $webZip
Compress-Archive -Path "$apiPublishDir\*" -DestinationPath $apiZip

Write-Host "Deploying applications..."
az webapp deploy --resource-group $ResourceGroup --name $WebAppName --src-path $webZip --type zip | Out-Null
az webapp deploy --resource-group $ResourceGroup --name $ApiAppName --src-path $apiZip --type zip | Out-Null

Write-Host "Deployment helper completed."
Write-Host "Web URL: https://$WebAppName.azurewebsites.net"
Write-Host "API URL: https://$ApiAppName.azurewebsites.net"
