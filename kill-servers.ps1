#!/usr/bin/env pwsh
# Script to kill API and Angular development servers

Write-Host "Checking for running servers..." -ForegroundColor Yellow

# API Server ports (5172, 7268)
$apiPorts = @(5172, 7268)
# Angular server port
$angularPort = 4200

$killedProcesses = 0

foreach ($port in ($apiPorts + $angularPort)) {
    Write-Host "`nChecking port $port..." -ForegroundColor Cyan
    
    # Find processes using the port
    $connections = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    
    if ($connections) {
        $processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
        
        foreach ($processId in $processIds) {
            if ($processId -gt 0) {
                try {
                    $process = Get-Process -Id $processId -ErrorAction Stop
                    $processName = $process.ProcessName
                    
                    Write-Host "  Found: $processName (PID: $processId) on port $port" -ForegroundColor Yellow
                    
                    # Kill the process
                    Stop-Process -Id $processId -Force -ErrorAction Stop
                    Write-Host "  Killed: $processName (PID: $processId)" -ForegroundColor Green
                    $killedProcesses++
                }
                catch {
                    Write-Host "  Failed to kill process PID: $processId - $_" -ForegroundColor Red
                }
            }
        }
    }
    else {
        Write-Host "  No process found on port $port" -ForegroundColor Gray
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
if ($killedProcesses -gt 0) {
    Write-Host "Successfully killed $killedProcesses process(es)" -ForegroundColor Green
    Write-Host "Servers have been shut down. You can now run your build." -ForegroundColor Green
}
else {
    Write-Host "No servers were running. Ready to build!" -ForegroundColor Green
}
Write-Host "========================================`n" -ForegroundColor Cyan