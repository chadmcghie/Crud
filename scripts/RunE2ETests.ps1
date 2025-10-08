#!/usr/bin/env pwsh
# Script to run E2E tests with API server management
# Cross-platform: Works on Windows, macOS, and Linux

param(
    [ValidateSet("smoke", "critical", "all")]
    [string]$TestSuite = "all",
    [string]$ApiProject = ".\src\Api\Api.csproj",
    [string]$TestProject = ".\test\Tests.E2E.NG",
    [string]$ApiUrl = "http://localhost:5172",
    [int]$ApiTimeout = 60,
    [int]$Delay = 2,
    [switch]$SkipCleanup,
    [switch]$Headed
)

Write-Host "Starting End to End Tests" -ForegroundColor Green
Write-Host ""

# Function to check if API is responding
function Test-ApiHealth {
    param([string]$Url)
    try {
        $response = Invoke-WebRequest -Uri "$Url/health" -UseBasicParsing -TimeoutSec 5
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

# Function to cleanup running servers
function Stop-TestServers {
    Write-Host "🧹 Cleaning up running servers..." -ForegroundColor Yellow
    try {
        & ".\scripts\kill-servers.ps1"
        Write-Host "✅ Server cleanup completed" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Server cleanup encountered issues: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# Function to start API server in background (cross-platform)
function Start-ApiServer {
    Write-Host "📡 Starting API server for testing..." -ForegroundColor Yellow

    # Start API with testing profile
    $apiArgs = @("run", "--project", $ApiProject, "--launch-profile", "testing")

    if ($IsWindows) {
        # Windows: Use WindowStyle Hidden
        $apiProcess = Start-Process "dotnet" -ArgumentList $apiArgs -PassThru -WindowStyle Hidden
    }
    else {
        # macOS/Linux: Start process in background (no WindowStyle parameter)
        $apiProcess = Start-Process "dotnet" -ArgumentList $apiArgs -PassThru
    }

    if ($apiProcess) {
        Write-Host "✅ API server started with PID: $($apiProcess.Id)" -ForegroundColor Green
        return $apiProcess
    } else {
        Write-Host "❌ Failed to start API server" -ForegroundColor Red
        return $null
    }
}

# Function to wait for API readiness
function Wait-ForApiReady {
    param([string]$Url, [int]$TimeoutSeconds)

    Write-Host "⏳ Waiting for API at $Url/health ..." -ForegroundColor Yellow
    $elapsed = 0

    while ($elapsed -lt $TimeoutSeconds) {
        if (Test-ApiHealth -Url $Url) {
            Write-Host "✅ API is ready and responding!" -ForegroundColor Green
            return $true
        }

        Write-Host "   Still waiting... ($elapsed/$TimeoutSeconds seconds)" -ForegroundColor Gray
        Start-Sleep -Seconds $Delay
        $elapsed += $Delay
    }

    Write-Host "❌ API did not respond within $TimeoutSeconds seconds" -ForegroundColor Red
    return $false
}

# Function to run E2E tests
function Invoke-E2ETests {
    param([string]$Suite, [string]$ProjectPath, [bool]$HeadedMode)

    Write-Host "🚀 Running E2E tests: $Suite" -ForegroundColor Cyan
    Write-Host ""

    # Change to test directory
    Push-Location $ProjectPath

    try {
        # Determine which test command to run
        $testCommand = switch ($Suite.ToLower()) {
            "smoke" { "npm run test:smoke" }
            "critical" { "npm run test:critical" }
            "all" { "npm run test" }
            default { "npm run test:smoke" }
        }

        # Add headed mode if requested
        if ($HeadedMode) {
            $testCommand += " -- --headed"
        }

        Write-Host "🔧 Executing: $testCommand" -ForegroundColor Gray
        Invoke-Expression $testCommand

        return $LASTEXITCODE
    } finally {
        Pop-Location
    }
}

# Main execution flow
try {
    Write-Host "📊 Test Configuration:" -ForegroundColor Cyan
    Write-Host "   Test Suite: $TestSuite" -ForegroundColor White
    Write-Host "   API Project: $ApiProject" -ForegroundColor White
    Write-Host "   Test Project: $TestProject" -ForegroundColor White
    Write-Host "   API URL: $ApiUrl" -ForegroundColor White
    Write-Host "   Headed Mode: $($Headed.IsPresent)" -ForegroundColor White
    Write-Host ""

    # Step 1: Cleanup existing servers unless skipped
    if (-not $SkipCleanup) {
        Stop-TestServers
        Start-Sleep -Seconds 2
    } else {
        Write-Host "⚠️ Skipping server cleanup (manual mode)" -ForegroundColor Yellow
    }

    # Step 2: Start API server
    $apiProcess = Start-ApiServer
    if (-not $apiProcess) {
        Write-Host "❌ Cannot proceed without API server" -ForegroundColor Red
        exit 1
    }

    # Step 3: Wait for API to be ready
    $apiReady = Wait-ForApiReady -Url $ApiUrl -TimeoutSeconds $ApiTimeout
    if (-not $apiReady) {
        Write-Host "❌ API server is not responding, cannot run tests" -ForegroundColor Red
        if ($apiProcess -and -not $apiProcess.HasExited) {
            Write-Host "🛑 Stopping API server..." -ForegroundColor Yellow
            $apiProcess.Kill()
        }
        exit 1
    }

    # Step 4: Run the E2E tests
    Write-Host ""
    $testResult = Invoke-E2ETests -Suite $TestSuite -ProjectPath $TestProject -HeadedMode $Headed.IsPresent

    # Step 5: Report results
    Write-Host ""
    if ($testResult -eq 0 -or $testResult -eq $null) {
        Write-Host "🎉 E2E Tests completed successfully!" -ForegroundColor Green
        $exitCode = 0
    } else {
        Write-Host "❌ E2E Tests failed with exit code: $testResult" -ForegroundColor Red
        $exitCode = $testResult
    }

} catch {
    Write-Host "💥 Script execution failed: $($_.Exception.Message)" -ForegroundColor Red
    $exitCode = 1
} finally {
    # Cleanup: Stop API server
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Write-Host ""
        Write-Host "🛑 Stopping API server (PID: $($apiProcess.Id))..." -ForegroundColor Yellow
        try {
            $apiProcess.Kill()
            $apiProcess.WaitForExit(5000)
            Write-Host "✅ API server stopped" -ForegroundColor Green
        } catch {
            Write-Host "⚠️ API server cleanup encountered issues: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "📋 Test session completed" -ForegroundColor Cyan
    Write-Host ""
}

exit $exitCode