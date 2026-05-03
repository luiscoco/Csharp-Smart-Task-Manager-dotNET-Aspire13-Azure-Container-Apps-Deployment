param(
    [string]$WebAppClientId = "ffdda8ba-1389-4fa9-bba5-b06d14ef55e5",

    [string]$ProjectPath = ".\src\SmartTaskManager.Web\SmartTaskManager.Web.csproj",

    [string]$DisplayName = "SmartTaskManager.Web rotated secret",

    [int]$Years = 1,

    [string]$ResourceGroup,

    [string]$WebAppName,

    [switch]$RestartWebApp
)

$ErrorActionPreference = "Stop"

Write-Host "Listing existing credential metadata..."
$beforeCredentials = az ad app credential list --id $WebAppClientId -o json | ConvertFrom-Json
$beforeKeyIds = @($beforeCredentials | ForEach-Object { $_.keyId })

Write-Host "Creating a new client secret with --append..."
$secretResponseJson = az ad app credential reset `
  --id $WebAppClientId `
  --append `
  --display-name $DisplayName `
  --years $Years `
  -o json

$secretResponse = $secretResponseJson | ConvertFrom-Json
$newClientSecret = $secretResponse.password

if ([string]::IsNullOrWhiteSpace($newClientSecret)) {
    throw "The new client secret was not returned by Azure CLI."
}

Write-Host "Writing the new client secret to local user secrets..."
dotnet user-secrets set "AzureAd:ClientSecret" $newClientSecret --project $ProjectPath | Out-Null

Write-Host "Listing credential metadata after rotation..."
$afterCredentials = az ad app credential list --id $WebAppClientId -o json | ConvertFrom-Json
$newCredential = $afterCredentials | Where-Object { $_.keyId -notin $beforeKeyIds } | Select-Object -First 1
$newCredentialKeyId = $newCredential.keyId

if (-not [string]::IsNullOrWhiteSpace($ResourceGroup) -and -not [string]::IsNullOrWhiteSpace($WebAppName)) {
    Write-Host "Writing the new client secret to Azure App Service settings..."
    az webapp config appsettings set `
      --resource-group $ResourceGroup `
      --name $WebAppName `
      --settings AzureAd__ClientSecret="$newClientSecret" | Out-Null

    if ($RestartWebApp) {
        Write-Host "Restarting the Azure Web App..."
        az webapp restart --resource-group $ResourceGroup --name $WebAppName | Out-Null
    }
}
else {
    Write-Host "Azure Web App update skipped because ResourceGroup or WebAppName was not provided."
}

Write-Host "Rotation helper completed."
Write-Host "New credential key ID: $newCredentialKeyId"
Write-Host "Local user secrets were updated for: $ProjectPath"
Write-Host "The client secret value was kept in memory and was not written to tracked source files."
