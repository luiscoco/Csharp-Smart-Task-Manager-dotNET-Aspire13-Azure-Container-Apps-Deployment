[CmdletBinding()]
param(
    [string]$SourceConnectionString = "Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True",

    [string]$TargetConnectionString = "Server=tcp:<sql-server-name>.database.windows.net,1433;Initial Catalog=SmartTaskManagerDb;Persist Security Info=False;User ID=<sql-admin-user>;Password=<sql-admin-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",

    [ValidateSet("BeforeImport", "AfterImport")]
    [string]$Mode = "AfterImport",

    [switch]$Execute
)

$ErrorActionPreference = "Stop"

$tableNames = @("Users", "Tasks", "TaskHistoryEntries")

$countQuery = @"
SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users
UNION ALL
SELECT 'Tasks', COUNT(*) FROM Tasks
UNION ALL
SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries
"@

$migrationHistoryQuery = "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory ORDER BY MigrationId;"
$migrationCountQuery = "SELECT COUNT(*) FROM __EFMigrationsHistory;"
$targetEmptyQuery = "SELECT COUNT(*) AS UserObjectCount FROM sys.objects WHERE is_ms_shipped = 0;"
$targetTablesQuery = "SELECT name FROM sys.tables ORDER BY name;"
$serviceObjectiveQuery = "SELECT edition, service_objective FROM sys.database_service_objectives WHERE database_id = DB_ID();"

function Invoke-ScalarQuery {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString,

        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.ExecuteScalar()
    }
    finally {
        $connection.Dispose()
    }
}

function Invoke-RowQuery {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString,

        [Parameter(Mandatory = $true)]
        [string]$Query
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Query

        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
        $table = New-Object System.Data.DataTable
        [void]$adapter.Fill($table)
        $table
    }
    finally {
        $connection.Dispose()
    }
}

function Convert-ToCountMap {
    param(
        [Parameter(Mandatory = $true)]
        [System.Data.DataTable]$Rows
    )

    $map = @{}
    foreach ($row in $Rows.Rows) {
        $map[$row["TableName"]] = [int]$row["RowCount"]
    }

    $map
}

function Assert-NoPlaceholderConnectionString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionString,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($ConnectionString -match "<[^>]+>") {
        throw "$Name contains placeholder values. Supply the real connection string when using -Execute."
    }
}

$runMode = if ($Execute) { "EXECUTE" } else { "DRY RUN" }

Write-Host "Azure SQL migration validation plan"
Write-Host "Mode: $runMode"
Write-Host "Validation mode: $Mode"

if (-not $Execute) {
    Write-Host ""
    Write-Host "Dry run only. Planned checks:"

    if ($Mode -eq "BeforeImport") {
        Write-Host "- Target empty check:"
        Write-Host $targetEmptyQuery
    }
    else {
        Write-Host "- Target table check:"
        Write-Host $targetTablesQuery
        Write-Host "- Source and target row count comparison:"
        Write-Host $countQuery
        Write-Host "- EF migration history comparison:"
        Write-Host $migrationHistoryQuery
        Write-Host "- Target service objective check:"
        Write-Host $serviceObjectiveQuery
    }

    Write-Host ""
    Write-Host "Re-run with -Execute to query the databases."
    return
}

Assert-NoPlaceholderConnectionString -ConnectionString $TargetConnectionString -Name "TargetConnectionString"

if ($Mode -eq "BeforeImport") {
    $targetObjectCount = Invoke-ScalarQuery -ConnectionString $TargetConnectionString -Query $targetEmptyQuery
    Write-Host "Target user-defined object count: $targetObjectCount"

    if ([int]$targetObjectCount -ne 0) {
        throw "Target database is not empty. Do not import until user-defined objects are removed or a fresh empty database is created."
    }

    Write-Host "Target database is empty and ready for BACPAC import."
    return
}

Assert-NoPlaceholderConnectionString -ConnectionString $SourceConnectionString -Name "SourceConnectionString"

$targetTables = Invoke-RowQuery -ConnectionString $TargetConnectionString -Query $targetTablesQuery
$sourceCounts = Invoke-RowQuery -ConnectionString $SourceConnectionString -Query $countQuery
$targetCounts = Invoke-RowQuery -ConnectionString $TargetConnectionString -Query $countQuery
$sourceMigrationRows = Invoke-RowQuery -ConnectionString $SourceConnectionString -Query $migrationHistoryQuery
$targetMigrationRows = Invoke-RowQuery -ConnectionString $TargetConnectionString -Query $migrationHistoryQuery
$sourceMigrationCount = Invoke-ScalarQuery -ConnectionString $SourceConnectionString -Query $migrationCountQuery
$targetMigrationCount = Invoke-ScalarQuery -ConnectionString $TargetConnectionString -Query $migrationCountQuery
$serviceObjective = Invoke-RowQuery -ConnectionString $TargetConnectionString -Query $serviceObjectiveQuery

Write-Host ""
Write-Host "Target tables:"
$targetTables | Format-Table -AutoSize

Write-Host "Source row counts:"
$sourceCounts | Format-Table -AutoSize

Write-Host "Target row counts:"
$targetCounts | Format-Table -AutoSize

Write-Host "Source EF migration history:"
$sourceMigrationRows | Format-Table -AutoSize

Write-Host "Target EF migration history:"
$targetMigrationRows | Format-Table -AutoSize

Write-Host "Target service objective:"
$serviceObjective | Format-Table -AutoSize

$sourceMap = Convert-ToCountMap -Rows $sourceCounts
$targetMap = Convert-ToCountMap -Rows $targetCounts

$mismatchFound = $false
foreach ($tableName in $tableNames) {
    if ($sourceMap[$tableName] -ne $targetMap[$tableName]) {
        Write-Host "Mismatch detected for ${tableName}: source=$($sourceMap[$tableName]) target=$($targetMap[$tableName])"
        $mismatchFound = $true
    }
}

if ([int]$sourceMigrationCount -ne [int]$targetMigrationCount) {
    Write-Host "Mismatch detected for __EFMigrationsHistory: source=$sourceMigrationCount target=$targetMigrationCount"
    $mismatchFound = $true
}

if ($mismatchFound) {
    throw "Validation failed. Source and target counts do not match."
}

Write-Host "Validation passed for table counts and EF migration history count."
