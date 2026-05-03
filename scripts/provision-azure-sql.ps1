param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,

    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,

    [Parameter(Mandatory = $true)]
    [string]$SqlAdminUser,

    [Parameter(Mandatory = $true)]
    [string]$SqlAdminPassword,

    [switch]$UseFreeOffer,

    [ValidateSet("AutoPause", "BillOverUsage")]
    [string]$FreeLimitExhaustionBehavior = "AutoPause",

    [double]$MinCapacity = 0.5,

    [int]$MaxCapacity = 1,

    [int]$AutoPauseDelay = 60,

    [string]$BackupStorageRedundancy = "Local",

    [string]$PublicIp
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($PublicIp)) {
    $PublicIp = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
}

Write-Host "Creating resource group..."
az group create --name $ResourceGroup --location $Location | Out-Null

Write-Host "Creating Azure SQL logical server..."
az sql server create `
  --resource-group $ResourceGroup `
  --name $SqlServerName `
  --location $Location `
  --admin-user $SqlAdminUser `
  --admin-password $SqlAdminPassword | Out-Null

Write-Host "Adding firewall rule for current client IP..."
az sql server firewall-rule create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name "AllowCurrentClientIp" `
  --start-ip-address $PublicIp `
  --end-ip-address $PublicIp | Out-Null

Write-Host "Adding firewall rule for Azure services..."
az sql server firewall-rule create `
  --resource-group $ResourceGroup `
  --server $SqlServerName `
  --name "AllowAzureServices" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0 | Out-Null

Write-Host "Creating Azure SQL Database..."

if ($UseFreeOffer) {
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
else {
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
      --backup-storage-redundancy $BackupStorageRedundancy | Out-Null
}

$connectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$SqlAdminUser;Password=<redacted>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "Provisioning completed."
Write-Host "SQL server: $SqlServerName"
Write-Host "Database: $DatabaseName"
Write-Host "Connection string template:"
Write-Host $connectionString
