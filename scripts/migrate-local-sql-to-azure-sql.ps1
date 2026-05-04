[CmdletBinding()]
param(
    [string]$SourceConnectionString = "Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True",

    [string]$TargetConnectionString,

    [string]$TargetServerName,

    [string]$TargetDatabaseName = "SmartTaskManagerDb",

    [string]$TargetUser,

    [securestring]$TargetPassword,

    [string]$BacpacPath = "C:\tmp\SmartTaskManagerDb.bacpac",

    [ValidateSet("ExportImport", "ExportOnly", "ImportOnly")]
    [string]$Mode = "ExportImport",

    [int]$CommandTimeoutSeconds = 1200,

    [switch]$Execute
)

$ErrorActionPreference = "Stop"

function ConvertTo-PlainText {
    param(
        [Parameter(Mandatory = $true)]
        [securestring]$Value
    )

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

function Invoke-PlannedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$DisplayCommand,

        [Parameter(Mandatory = $true)]
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "## $Description"
    Write-Host $DisplayCommand

    if ($Execute) {
        & $Command
    }
}

function Test-ShouldExport {
    $Mode -eq "ExportImport" -or $Mode -eq "ExportOnly"
}

function Test-ShouldImport {
    $Mode -eq "ExportImport" -or $Mode -eq "ImportOnly"
}

if ($Execute) {
    $sqlPackage = Get-Command sqlpackage -ErrorAction SilentlyContinue
    if (-not $sqlPackage) {
        throw "sqlpackage was not found on PATH. Install it first with: dotnet tool install -g microsoft.sqlpackage"
    }
}

if ((Test-ShouldImport) -and [string]::IsNullOrWhiteSpace($TargetConnectionString)) {
    if ([string]::IsNullOrWhiteSpace($TargetServerName) -or
        [string]::IsNullOrWhiteSpace($TargetDatabaseName) -or
        [string]::IsNullOrWhiteSpace($TargetUser)) {
        throw "For import, supply either -TargetConnectionString or -TargetServerName, -TargetDatabaseName, and -TargetUser."
    }

    if ($Execute -and -not $TargetPassword) {
        throw "TargetPassword is required when importing with target server components and -Execute."
    }

    $passwordForConnectionString = if ($Execute) {
        ConvertTo-PlainText -Value $TargetPassword
    }
    else {
        "<sql-admin-password>"
    }

    $TargetConnectionString = "Server=tcp:$TargetServerName.database.windows.net,1433;Initial Catalog=$TargetDatabaseName;Persist Security Info=False;User ID=$TargetUser;Password=$passwordForConnectionString;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

$targetConnectionStringForDisplay = $TargetConnectionString
if (-not [string]::IsNullOrWhiteSpace($targetConnectionStringForDisplay)) {
    $targetConnectionStringForDisplay = $targetConnectionStringForDisplay -replace "Password=[^;]*", "Password=<redacted>"
}

$runMode = if ($Execute) { "EXECUTE" } else { "DRY RUN" }

Write-Host "Azure SQL BACPAC migration plan"
Write-Host "Mode: $runMode"
Write-Host "Migration mode: $Mode"
Write-Host "BACPAC path: $BacpacPath"

if (Test-ShouldExport) {
    Invoke-PlannedCommand `
        -Description "Ensure BACPAC folder exists" `
        -DisplayCommand "New-Item -ItemType Directory -Path `"$([IO.Path]::GetDirectoryName($BacpacPath))`" -Force" `
        -Command {
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($BacpacPath)) -Force | Out-Null
        }

    Invoke-PlannedCommand `
        -Description "Export local SQL Server database to BACPAC" `
        -DisplayCommand "sqlpackage /Action:Export /SourceConnectionString:`"$SourceConnectionString`" /TargetFile:`"$BacpacPath`" /p:CommandTimeout=$CommandTimeoutSeconds /p:VerifyExtraction=True" `
        -Command {
            sqlpackage /Action:Export `
                /SourceConnectionString:"$SourceConnectionString" `
                /TargetFile:"$BacpacPath" `
                /p:CommandTimeout=$CommandTimeoutSeconds `
                /p:VerifyExtraction=True
        }
}

if (Test-ShouldImport) {
    Invoke-PlannedCommand `
        -Description "Import BACPAC into empty Azure SQL Database" `
        -DisplayCommand "sqlpackage /Action:Import /SourceFile:`"$BacpacPath`" /TargetConnectionString:`"$targetConnectionStringForDisplay`" /p:CommandTimeout=$CommandTimeoutSeconds" `
        -Command {
            sqlpackage /Action:Import `
                /SourceFile:"$BacpacPath" `
                /TargetConnectionString:"$TargetConnectionString" `
                /p:CommandTimeout=$CommandTimeoutSeconds
        }
}

if (-not $Execute) {
    Write-Host ""
    Write-Host "Dry run only. Re-run with -Execute after writes are frozen and the target database is confirmed empty."
}
