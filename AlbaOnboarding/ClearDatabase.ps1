# Script to delete the development database
# This allows Entity Framework to recreate it cleanly with migrations

Write-Host "Deleting the development database..." -ForegroundColor Yellow

# Get the connection string from appsettings.json
$appsettings = Get-Content "appsettings.json" | ConvertFrom-Json
$connectionString = $appsettings.ConnectionStrings.DefaultConnection

# Extract database name from connection string
if ($connectionString -match "Database=([^;]+)") {
    $dbName = $matches[1]
    Write-Host "Database name: $dbName" -ForegroundColor Cyan
    
    # Build connection string for master database
    $masterConnectionString = $connectionString -replace "Database=[^;]+", "Database=master"
    
    # SQL to drop the database
    $dropSQL = @"
    IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$dbName')
    BEGIN
        ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [$dbName];
    END
"@
    
    # Execute the drop command
    try {
        Invoke-Sqlcmd -ConnectionString $masterConnectionString -Query $dropSQL -ErrorAction Stop
        Write-Host "Database '$dbName' deleted successfully!" -ForegroundColor Green
        Write-Host "`nNext steps:" -ForegroundColor Yellow
        Write-Host "1. Run the application" -ForegroundColor White
        Write-Host "2. Migrations will automatically create the database" -ForegroundColor White
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
        Write-Host "`nAlternatively, you can:" -ForegroundColor Yellow
        Write-Host "1. Open SQL Server Management Studio" -ForegroundColor White
        Write-Host "2. Delete the database: $dbName" -ForegroundColor White
        Write-Host "3. Run the application again" -ForegroundColor White
    }
}
else {
    Write-Host "Could not extract database name from connection string" -ForegroundColor Red
}
