param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "SmartTaskManager.sln"
$webProject = Join-Path $repoRoot "src\SmartTaskManager.Web\SmartTaskManager.Web.csproj"
$apiProject = Join-Path $repoRoot "src\SmartTaskManager.Api\SmartTaskManager.Api.csproj"
$webLaunchSettings = Join-Path $repoRoot "src\SmartTaskManager.Web\Properties\launchSettings.json"
$apiLaunchSettings = Join-Path $repoRoot "src\SmartTaskManager.Api\Properties\launchSettings.json"

function Get-TargetFramework {
    param([string]$ProjectPath)

    if (-not (Test-Path $ProjectPath)) {
        return $null
    }

    [xml]$xml = Get-Content -LiteralPath $ProjectPath
    return $xml.Project.PropertyGroup.TargetFramework | Select-Object -First 1
}

function Get-LaunchUrl {
    param(
        [string]$LaunchSettingsPath,
        [string]$ProfileName
    )

    if (-not (Test-Path $LaunchSettingsPath)) {
        return $null
    }

    $json = Get-Content -LiteralPath $LaunchSettingsPath -Raw | ConvertFrom-Json
    return $json.profiles.$ProfileName.applicationUrl
}

$solutionExists = Test-Path $solutionPath
$webTargetFramework = Get-TargetFramework -ProjectPath $webProject
$apiTargetFramework = Get-TargetFramework -ProjectPath $apiProject
$webHttpsUrl = Get-LaunchUrl -LaunchSettingsPath $webLaunchSettings -ProfileName "https"
$apiHttpsUrl = Get-LaunchUrl -LaunchSettingsPath $apiLaunchSettings -ProfileName "https"

$staleDocs = @()
$matches = rg -n "localhost:5001" (Join-Path $repoRoot "docs") 2>$null
if ($matches) {
    $staleDocs = $matches
}

[pscustomobject]@{
    SolutionExists = $solutionExists
    SolutionPath = $solutionPath
    WebTargetFramework = $webTargetFramework
    ApiTargetFramework = $apiTargetFramework
    WebHttpsLaunchUrl = $webHttpsUrl
    ApiHttpsLaunchUrl = $apiHttpsUrl
    StaleLocalhost5001Mentions = $staleDocs.Count
} | Format-List

if ($staleDocs.Count -gt 0) {
    Write-Host ""
    Write-Host "Docs that still mention localhost:5001:"
    $staleDocs | ForEach-Object { Write-Host $_ }
}

Write-Host ""
Write-Host "Recommended migration policy:"
Write-Host "- Keep SmartTaskManager.Web on https://localhost:7036 for the first Aspire migration."
Write-Host "- Keep SmartTaskManager.Api on an external connection string."
Write-Host "- Prefer manual AppHost and ServiceDefaults creation under src."
