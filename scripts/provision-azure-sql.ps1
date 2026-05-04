[CmdletBinding()]
param(
    [string]$ResourceGroup = "rg-smarttaskmanager-data-dev",

    [string]$Location = "westeurope",

    [string]$SqlServerName = "sqlstmdev001",

    [string]$DatabaseName = "SmartTaskManagerDb",

    [string]$SqlAdminUser = "sqladminstm",

    [securestring]$SqlAdminPassword,

    [ValidateSet("Free", "Basic", "S0")]
    [string]$Sku = "Free",

    [ValidateSet("AutoPause", "BillOverUsage")]
    [string]$FreeLimitExhaustionBehavior = "AutoPause",

    [double]$MinCapacity = 0.5,

    [int]$MaxCapacity = 1,

    [int]$AutoPauseDelay = 60,

    [ValidateSet("Local", "Zone", "Geo", "GeoZone")]
    [string]$BackupStorageRedundancy = "Local",

    [string]$PublicIp,

    [switch]$AllowAzureServices,

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

if ($Execute -and -not $SqlAdminPassword) {
    throw "SqlAdminPassword is required when -Execute is supplied."
}

$sqlAdminPasswordPlain = $null
if ($Execute) {
    $sqlAdminPasswordPlain = ConvertTo-PlainText -Value $SqlAdminPassword
}

if ([string]::IsNullOrWhiteSpace($PublicIp)) {
    if ($Execute) {
        $PublicIp = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
    }
    else {
        $PublicIp = "<current-public-ip>"
    }
}

$runMode = if ($Execute) { "EXECUTE" } else { "DRY RUN" }

Write-Host "Azure SQL provisioning plan"
Write-Host "Mode: $runMode"
Write-Host "SKU: $Sku"
Write-Host "Resource group: $ResourceGroup"
Write-Host "Location: $Location"
Write-Host "Logical server: $SqlServerName"
Write-Host "Database: $DatabaseName"

Invoke-PlannedCommand `
    -Description "Create resource group" `
    -DisplayCommand "az group create --name $ResourceGroup --location $Location" `
    -Command {
        az group create `
            --name $ResourceGroup `
            --location $Location | Out-Null
    }

Invoke-PlannedCommand `
    -Description "Create Azure SQL logical server" `
    -DisplayCommand "az sql server create --resource-group $ResourceGroup --name $SqlServerName --location $Location --admin-user $SqlAdminUser --admin-password <redacted>" `
    -Command {
        az sql server create `
            --resource-group $ResourceGroup `
            --name $SqlServerName `
            --location $Location `
            --admin-user $SqlAdminUser `
            --admin-password $sqlAdminPasswordPlain | Out-Null
    }

Invoke-PlannedCommand `
    -Description "Allow current client public IP" `
    -DisplayCommand "az sql server firewall-rule create --resource-group $ResourceGroup --server $SqlServerName --name AllowCurrentClientIp --start-ip-address $PublicIp --end-ip-address $PublicIp" `
    -Command {
        az sql server firewall-rule create `
            --resource-group $ResourceGroup `
            --server $SqlServerName `
            --name "AllowCurrentClientIp" `
            --start-ip-address $PublicIp `
            --end-ip-address $PublicIp | Out-Null
    }

if ($AllowAzureServices) {
    Invoke-PlannedCommand `
        -Description "Allow Azure services" `
        -DisplayCommand "az sql server firewall-rule create --resource-group $ResourceGroup --server $SqlServerName --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0" `
        -Command {
            az sql server firewall-rule create `
                --resource-group $ResourceGroup `
                --server $SqlServerName `
                --name "AllowAzureServices" `
                --start-ip-address 0.0.0.0 `
                --end-ip-address 0.0.0.0 | Out-Null
        }
}
else {
    Write-Host ""
    Write-Host "## Allow Azure services"
    Write-Host "Skipped. Supply -AllowAzureServices if an Azure-hosted API needs the broad 0.0.0.0 Azure SQL firewall rule."
}

switch ($Sku) {
    "Free" {
        Invoke-PlannedCommand `
            -Description "Create Azure SQL Database with free limit" `
            -DisplayCommand "az sql db create --resource-group $ResourceGroup --server $SqlServerName --name $DatabaseName --edition GeneralPurpose --family Gen5 --capacity $MaxCapacity --compute-model Serverless --min-capacity $MinCapacity --auto-pause-delay $AutoPauseDelay --backup-storage-redundancy $BackupStorageRedundancy --use-free-limit --free-limit-exhaustion-behavior $FreeLimitExhaustionBehavior" `
            -Command {
                az sql db create `
                    --resource-group $ResourceGroup `
                    --server $SqlServerName `
                    --name $DatabaseName `
                    --edition GeneralPurpose `
                    --family Gen5 `
                    --capacity $MaxCapacity `
                    --compute-model Serverless `
                    --min-capacity $MinCapacity `
                    --auto-pause-delay $AutoPauseDelay `
                    --backup-storage-redundancy $BackupStorageRedundancy `
                    --use-free-limit `
                    --free-limit-exhaustion-behavior $FreeLimitExhaustionBehavior | Out-Null
            }
    }
    "Basic" {
        Invoke-PlannedCommand `
            -Description "Create Basic Azure SQL Database paid fallback" `
            -DisplayCommand "az sql db create --resource-group $ResourceGroup --server $SqlServerName --name $DatabaseName --edition Basic --service-objective Basic --max-size 2GB --backup-storage-redundancy $BackupStorageRedundancy" `
            -Command {
                az sql db create `
                    --resource-group $ResourceGroup `
                    --server $SqlServerName `
                    --name $DatabaseName `
                    --edition Basic `
                    --service-objective Basic `
                    --max-size 2GB `
                    --backup-storage-redundancy $BackupStorageRedundancy | Out-Null
            }
    }
    "S0" {
        Invoke-PlannedCommand `
            -Description "Create S0 Azure SQL Database fallback" `
            -DisplayCommand "az sql db create --resource-group $ResourceGroup --server $SqlServerName --name $DatabaseName --edition Standard --service-objective S0 --max-size 250GB --backup-storage-redundancy $BackupStorageRedundancy" `
            -Command {
                az sql db create `
                    --resource-group $ResourceGroup `
                    --server $SqlServerName `
                    --name $DatabaseName `
                    --edition Standard `
                    --service-objective S0 `
                    --max-size 250GB `
                    --backup-storage-redundancy $BackupStorageRedundancy | Out-Null
            }
    }
}

$connectionStringTemplate = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$SqlAdminUser;Password=<redacted>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host ""
Write-Host "Connection string template:"
Write-Host $connectionStringTemplate

if (-not $Execute) {
    Write-Host ""
    Write-Host "Dry run only. Re-run with -Execute to create Azure resources."
}
