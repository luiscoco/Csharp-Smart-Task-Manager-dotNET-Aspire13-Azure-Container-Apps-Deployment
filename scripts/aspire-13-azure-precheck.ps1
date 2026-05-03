param()

$ErrorActionPreference = "Stop"

Write-Host "Aspire 13 Azure precheck"
Write-Host "This script is read-only. It does not run az, azd, aspire run, dotnet publish, or deployment commands."
Write-Host ""

$repoRoot = Split-Path -Parent $PSScriptRoot

$paths = [ordered]@{
    Solution = Join-Path $repoRoot "SmartTaskManager.sln"
    AppHostProject = Join-Path $repoRoot "src\SmartTaskManager.AppHost\SmartTaskManager.AppHost.csproj"
    AppHostCode = Join-Path $repoRoot "src\SmartTaskManager.AppHost\AppHost.cs"
    ServiceDefaultsProject = Join-Path $repoRoot "src\SmartTaskManager.ServiceDefaults\SmartTaskManager.ServiceDefaults.csproj"
    WebProject = Join-Path $repoRoot "src\SmartTaskManager.Web\SmartTaskManager.Web.csproj"
    ApiProject = Join-Path $repoRoot "src\SmartTaskManager.Api\SmartTaskManager.Api.csproj"
    AppServiceBicep = Join-Path $repoRoot "infra\appservice.bicep"
    AppServiceDeployScript = Join-Path $repoRoot "scripts\deploy-appservice.ps1"
    AzureStrategyDoc = Join-Path $repoRoot "docs\aspire-13-azure-strategy.md"
    AzureCommandsDoc = Join-Path $repoRoot "docs\aspire-13-azure-commands.md"
    AzureCostRiskDoc = Join-Path $repoRoot "docs\aspire-13-azure-cost-and-risk.md"
}

function Test-File {
    param([string]$Path)

    Test-Path -LiteralPath $Path -PathType Leaf
}

function Get-FileText {
    param([string]$Path)

    if (-not (Test-File -Path $Path)) {
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Test-TextContains {
    param(
        [string]$Text,
        [string]$Value,
        [System.StringComparison]$Comparison = [System.StringComparison]::Ordinal
    )

    return $Text.IndexOf($Value, $Comparison) -ge 0
}

function Get-ProjectSdk {
    param([string]$ProjectPath)

    if (-not (Test-File -Path $ProjectPath)) {
        return $null
    }

    [xml]$xml = Get-Content -LiteralPath $ProjectPath
    return $xml.Project.Sdk
}

function Get-TargetFramework {
    param([string]$ProjectPath)

    if (-not (Test-File -Path $ProjectPath)) {
        return $null
    }

    [xml]$xml = Get-Content -LiteralPath $ProjectPath
    return $xml.Project.PropertyGroup.TargetFramework | Select-Object -First 1
}

function Test-ProjectReference {
    param(
        [string]$ProjectPath,
        [string]$ReferenceFragment
    )

    $text = Get-FileText -Path $ProjectPath
    return Test-TextContains -Text $text -Value $ReferenceFragment -Comparison ([System.StringComparison]::OrdinalIgnoreCase)
}

Write-Host "Required files:"
foreach ($entry in $paths.GetEnumerator()) {
    $exists = Test-File -Path $entry.Value
    Write-Host ("- {0}: {1}" -f $entry.Key, $(if ($exists) { "found" } else { "missing" }))
}

Write-Host ""
Write-Host "Project facts:"
Write-Host ("- AppHost SDK: {0}" -f (Get-ProjectSdk -ProjectPath $paths.AppHostProject))
Write-Host ("- Web target framework: {0}" -f (Get-TargetFramework -ProjectPath $paths.WebProject))
Write-Host ("- API target framework: {0}" -f (Get-TargetFramework -ProjectPath $paths.ApiProject))

$appHostText = Get-FileText -Path $paths.AppHostCode
$appHostProjectText = Get-FileText -Path $paths.AppHostProject
$bicepText = Get-FileText -Path $paths.AppServiceBicep
$deployScriptText = Get-FileText -Path $paths.AppServiceDeployScript

$checks = [ordered]@{
    "AppHost models API project" = Test-TextContains -Text $appHostText -Value "SmartTaskManager_Api"
    "AppHost models Web project" = Test-TextContains -Text $appHostText -Value "SmartTaskManager_Web"
    "AppHost injects API base URL" = Test-TextContains -Text $appHostText -Value "SmartTaskManagerApi__BaseUrl"
    "AppHost does not use Azure App Service integration" = -not (Test-TextContains -Text $appHostText -Value "AddAzureAppServiceEnvironment")
    "AppHost does not publish as App Service website" = -not (Test-TextContains -Text $appHostText -Value "PublishAsAzureAppServiceWebsite")
    "AppHost project does not reference Aspire.Hosting.Azure.AppService" = -not (Test-TextContains -Text $appHostProjectText -Value "Aspire.Hosting.Azure.AppService")
    "Web references ServiceDefaults" = Test-ProjectReference -ProjectPath $paths.WebProject -ReferenceFragment "SmartTaskManager.ServiceDefaults"
    "API references ServiceDefaults" = Test-ProjectReference -ProjectPath $paths.ApiProject -ReferenceFragment "SmartTaskManager.ServiceDefaults"
    "Bicep keeps B1 SKU allowed" = Test-TextContains -Text $bicepText -Value "'B1'"
    "Deploy script defaults to B1" = Test-TextContains -Text $deployScriptText -Value '$Sku = "B1"'
}

Write-Host ""
Write-Host "Strategy checks:"
foreach ($entry in $checks.GetEnumerator()) {
    Write-Host ("- {0}: {1}" -f $entry.Key, $(if ($entry.Value) { "pass" } else { "review" }))
}

Write-Host ""
Write-Host "Recommended Azure path:"
Write-Host "- Keep Aspire local-only for now."
Write-Host "- Keep the current manual Azure App Service + Azure SQL deployment path."
Write-Host "- Do not add Aspire Azure App Service deployment until a separate preview proof-of-concept is approved."
