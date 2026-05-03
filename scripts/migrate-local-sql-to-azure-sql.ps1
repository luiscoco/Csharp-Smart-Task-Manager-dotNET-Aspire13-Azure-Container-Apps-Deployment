param(
    [string]$SourceConnectionString = "Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True",

    [Parameter(Mandatory = $true)]
    [string]$TargetServerName,

    [Parameter(Mandatory = $true)]
    [string]$TargetDatabaseName,

    [Parameter(Mandatory = $true)]
    [string]$TargetUser,

    [Parameter(Mandatory = $true)]
    [string]$TargetPassword,

    [string]$BacpacPath = "C:\tmp\SmartTaskManagerDb.bacpac",

    [int]$CommandTimeoutSeconds = 1200
)

$ErrorActionPreference = "Stop"

$sqlPackage = Get-Command sqlpackage -ErrorAction SilentlyContinue
if (-not $sqlPackage) {
    throw "sqlpackage was not found on PATH. Install it first with: dotnet tool install -g microsoft.sqlpackage"
}

$targetConnectionString = "Server=tcp:$TargetServerName.database.windows.net,1433;Initial Catalog=$TargetDatabaseName;Persist Security Info=False;User ID=$TargetUser;Password=$TargetPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "Exporting local database to BACPAC..."
sqlpackage /Action:Export `
  /SourceConnectionString:"$SourceConnectionString" `
  /TargetFile:"$BacpacPath" `
  /p:CommandTimeout=$CommandTimeoutSeconds `
  /p:VerifyExtraction=True

Write-Host "Importing BACPAC into Azure SQL Database..."
sqlpackage /Action:Import `
  /SourceFile:"$BacpacPath" `
  /TargetConnectionString:"$targetConnectionString" `
  /p:CommandTimeout=$CommandTimeoutSeconds `
  /p:DatabaseEdition=GeneralPurpose `
  /p:DatabaseServiceObjective=GP_S_Gen5_1

Write-Host "Migration commands completed. Validate row counts and migration history before cutover."
