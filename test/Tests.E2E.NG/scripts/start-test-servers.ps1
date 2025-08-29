# PowerShell script to start test servers for serial E2E testing
# Checks if servers are already running and reuses them if possible

param(
    [int]$ApiPort = 5172,
    [int]$AngularPort = 4200,
    [string]$Environment = "Testing",
    [switch]$Force
)

Write-Host "🚀 Starting test servers..." -ForegroundColor Green

# Function to check if port is in use
function Test-Port {
    param($Port)
    try {
        $connection = New-Object System.Net.Sockets.TcpClient
        $connection.Connect("localhost", $Port)
        $connection.Close()
        return $true
    } catch {
        return $false
    }
}

# Function to wait for server
function Wait-ForServer {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 30
    )
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    while ($stopwatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 404) {
                return $true
            }
        } catch {
            # Server not ready, continue waiting
        }
        Start-Sleep -Seconds 1
    }
    
    return $false
}

# Check if API server is already running
$apiRunning = Test-Port -Port $ApiPort
if ($apiRunning -and -not $Force) {
    Write-Host "✅ API server already running on port $ApiPort" -ForegroundColor Green
} else {
    if ($apiRunning) {
        Write-Host "⚠️ Stopping existing API server on port $ApiPort" -ForegroundColor Yellow
        Get-Process -Name "dotnet" | Where-Object { $_.MainWindowTitle -like "*Api*" } | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    
    Write-Host "🔧 Starting API server on port $ApiPort..." -ForegroundColor Cyan
    
    # Set up database path
    $tempPath = [System.IO.Path]::GetTempPath()
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $dbPath = Join-Path $tempPath "CrudTest_Serial_$timestamp.db"
    
    # Start API server
    $apiPath = Join-Path $PSScriptRoot "..\..\..\src\Api"
    $env:ASPNETCORE_URLS = "http://localhost:$ApiPort"
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    $env:DatabasePath = $dbPath
    $env:ConnectionStrings__DefaultConnection = "Data Source=$dbPath"
    $env:Logging__LogLevel__Default = "Warning"
    
    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--no-build" `
        -WorkingDirectory $apiPath `
        -PassThru `
        -WindowStyle Hidden
    
    # Wait for API to be ready
    Write-Host "⏳ Waiting for API server to start..." -ForegroundColor Yellow
    if (Wait-ForServer -Url "http://localhost:$ApiPort/health") {
        Write-Host "✅ API server is ready!" -ForegroundColor Green
    } else {
        Write-Host "❌ API server failed to start" -ForegroundColor Red
        exit 1
    }
}

# Check if Angular server is already running
$angularRunning = Test-Port -Port $AngularPort
if ($angularRunning -and -not $Force) {
    Write-Host "✅ Angular server already running on port $AngularPort" -ForegroundColor Green
} else {
    if ($angularRunning) {
        Write-Host "⚠️ Stopping existing Angular server on port $AngularPort" -ForegroundColor Yellow
        Get-Process -Name "node" | Where-Object { 
            $_.MainWindowTitle -like "*ng serve*" -or 
            $_.CommandLine -like "*ng serve*" 
        } | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    
    Write-Host "🔧 Starting Angular server on port $AngularPort..." -ForegroundColor Cyan
    
    # Start Angular server
    $angularPath = Join-Path $PSScriptRoot "..\..\..\src\Angular"
    $env:API_URL = "http://localhost:$ApiPort"
    
    $angularProcess = Start-Process -FilePath "npm" `
        -ArgumentList "run", "serve:test", "--", "--port", $AngularPort `
        -WorkingDirectory $angularPath `
        -PassThru `
        -WindowStyle Hidden
    
    # Wait for Angular to be ready
    Write-Host "⏳ Waiting for Angular server to start (this may take a minute)..." -ForegroundColor Yellow
    if (Wait-ForServer -Url "http://localhost:$AngularPort") {
        Write-Host "✅ Angular server is ready!" -ForegroundColor Green
    } else {
        Write-Host "❌ Angular server failed to start" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n✨ Test servers are ready!" -ForegroundColor Green
Write-Host "📍 API: http://localhost:$ApiPort" -ForegroundColor Cyan
Write-Host "📍 Angular: http://localhost:$AngularPort" -ForegroundColor Cyan

if ($dbPath) {
    Write-Host "📍 Database: $dbPath" -ForegroundColor Cyan
}

Write-Host "`n💡 Tip: Servers will keep running after tests for faster subsequent runs" -ForegroundColor Yellow
Write-Host "   Use -Force flag to restart servers even if already running" -ForegroundColor Yellow
Write-Host "   Run .\stop-test-servers.ps1 to stop them when done" -ForegroundColor Yellow