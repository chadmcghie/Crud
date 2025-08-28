# PowerShell script to stop test servers

param(
    [int]$ApiPort = 5172,
    [int]$AngularPort = 4200
)

Write-Host "🛑 Stopping test servers..." -ForegroundColor Yellow

# Function to kill process on port
function Stop-ProcessOnPort {
    param($Port)
    
    try {
        $netstat = netstat -ano | findstr :$Port
        if ($netstat) {
            $lines = $netstat -split "`n"
            foreach ($line in $lines) {
                if ($line -match "\s+(\d+)$") {
                    $pid = $matches[1]
                    if ($pid -and $pid -ne "0") {
                        try {
                            Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
                            Write-Host "✅ Stopped process $pid on port $Port" -ForegroundColor Green
                        } catch {
                            Write-Host "⚠️ Could not stop process $pid" -ForegroundColor Yellow
                        }
                    }
                }
            }
        } else {
            Write-Host "ℹ️ No process found on port $Port" -ForegroundColor Cyan
        }
    } catch {
        Write-Host "⚠️ Error checking port $Port" -ForegroundColor Yellow
    }
}

# Stop API server
Write-Host "Stopping API server on port $ApiPort..." -ForegroundColor Cyan
Stop-ProcessOnPort -Port $ApiPort

# Stop Angular server  
Write-Host "Stopping Angular server on port $AngularPort..." -ForegroundColor Cyan
Stop-ProcessOnPort -Port $AngularPort

# Also try to stop by process name as fallback
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.Path -like "*Api*" 
} | Stop-Process -Force -ErrorAction SilentlyContinue

Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object { 
    $_.Path -like "*Angular*" -or $_.CommandLine -like "*ng serve*" 
} | Stop-Process -Force -ErrorAction SilentlyContinue

# Clean up test databases
Write-Host "`nCleaning up test databases..." -ForegroundColor Cyan
$tempPath = [System.IO.Path]::GetTempPath()
$testDbs = Get-ChildItem -Path $tempPath -Filter "CrudTest_*.db" -ErrorAction SilentlyContinue

foreach ($db in $testDbs) {
    try {
        Remove-Item $db.FullName -Force
        Write-Host "🗑️ Deleted $($db.Name)" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Could not delete $($db.Name) (may be in use)" -ForegroundColor Yellow
    }
}

Write-Host "`n✅ Test server cleanup completed!" -ForegroundColor Green