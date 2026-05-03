param(
    [string]$SourceConnectionString = "Server=localhost;Database=SmartTaskManagerDb;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True",

    [Parameter(Mandatory = $true)]
    [string]$TargetConnectionString
)

$ErrorActionPreference = "Stop"

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

$countQuery = @"
SELECT 'Users' AS TableName, COUNT(*) AS RowCount FROM Users
UNION ALL
SELECT 'Tasks', COUNT(*) FROM Tasks
UNION ALL
SELECT 'TaskHistoryEntries', COUNT(*) FROM TaskHistoryEntries
"@

$migrationQuery = "SELECT COUNT(*) FROM __EFMigrationsHistory;"

$sourceCounts = Invoke-RowQuery -ConnectionString $SourceConnectionString -Query $countQuery
$targetCounts = Invoke-RowQuery -ConnectionString $TargetConnectionString -Query $countQuery

$sourceMigrationCount = Invoke-ScalarQuery -ConnectionString $SourceConnectionString -Query $migrationQuery
$targetMigrationCount = Invoke-ScalarQuery -ConnectionString $TargetConnectionString -Query $migrationQuery

Write-Host "Source row counts:"
$sourceCounts | Format-Table -AutoSize

Write-Host "Target row counts:"
$targetCounts | Format-Table -AutoSize

Write-Host "Source migration rows: $sourceMigrationCount"
Write-Host "Target migration rows: $targetMigrationCount"

$sourceMap = @{}
foreach ($row in $sourceCounts.Rows) {
    $sourceMap[$row["TableName"]] = [int]$row["RowCount"]
}

$targetMap = @{}
foreach ($row in $targetCounts.Rows) {
    $targetMap[$row["TableName"]] = [int]$row["RowCount"]
}

$mismatchFound = $false
foreach ($tableName in @("Users", "Tasks", "TaskHistoryEntries")) {
    if ($sourceMap[$tableName] -ne $targetMap[$tableName]) {
        Write-Host "Mismatch detected for $tableName: source=$($sourceMap[$tableName]) target=$($targetMap[$tableName])"
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
