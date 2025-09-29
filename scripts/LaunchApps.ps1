#!/usr/bin/env pwsh
# Script to launch API and Angular development servers
# Cross-platform: Works on Windows, macOS, and Linux

param(
    [string]$ApiProject = ".\src\Api\Api.csproj",
    [string]$ApiUrl = "http://localhost:5172/swagger",
    [int]$Timeout = 60,
    [int]$Delay = 2
)

Write-Host "🚀 Starting API and Angular Development Environment" -ForegroundColor Green
Write-Host ""

# Function to check if API is responding
function Test-ApiHealth {
    param([string]$Url)
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

# Function to start process in new terminal (cross-platform)
function Start-ProcessInTerminal {
    param(
        [string]$Command,
        [string[]]$Arguments,
        [string]$WorkingDirectory = $null
    )

    if ($IsWindows) {
        # Windows: Use Start-Process with WindowStyle
        $params = @{
            FilePath = $Command
            ArgumentList = $Arguments
            PassThru = $true
            WindowStyle = "Normal"
        }
        if ($WorkingDirectory) {
            $params.WorkingDirectory = $WorkingDirectory
        }
        return Start-Process @params
    }
    elseif ($IsMacOS) {
        # macOS: Use osascript to open Terminal.app
        $argString = ($Arguments | ForEach-Object { "`"$_`"" }) -join ' '
        $script = "tell application `"Terminal`" to do script `"cd '$WorkingDirectory'; $Command $argString`""
        & osascript -e $script
        # Return a mock process object since osascript doesn't return PID easily
        return [PSCustomObject]@{ Id = -1 }
    }
    else {
        # Linux: Try common terminal emulators
        $terminals = @(
            @{ Cmd = "gnome-terminal"; Args = @("--", $Command) + $Arguments }
            @{ Cmd = "xterm"; Args = @("-e", $Command) + $Arguments }
            @{ Cmd = "konsole"; Args = @("-e", $Command) + $Arguments }
        )

        foreach ($term in $terminals) {
            if (Get-Command $term.Cmd -ErrorAction SilentlyContinue) {
                $params = @{
                    FilePath = $term.Cmd
                    ArgumentList = $term.Args
                    PassThru = $true
                }
                if ($WorkingDirectory) {
                    $params.WorkingDirectory = $WorkingDirectory
                }
                return Start-Process @params
            }
        }

        Write-Host "⚠️ No supported terminal emulator found. Starting in background..." -ForegroundColor Yellow
        return Start-Process $Command -ArgumentList $Arguments -PassThru
    }
}

# Start the API in a new terminal window
Write-Host "📡 Starting API from $ApiProject ..." -ForegroundColor Yellow
$apiArgs = @("run", "--project", $ApiProject, "--launch-profile", "http")
$apiProcess = Start-ProcessInTerminal -Command "dotnet" -Arguments $apiArgs

if ($apiProcess) {
    Write-Host "✅ API process started with PID: $($apiProcess.Id)" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to start API process" -ForegroundColor Red
    exit 1
}

# Wait for API to be ready
Write-Host "⏳ Waiting for API at $ApiUrl ..." -ForegroundColor Yellow
$elapsed = 0
$success = $false

while ($elapsed -lt $Timeout) {
    if (Test-ApiHealth -Url $ApiUrl) {
        Write-Host "✅ API is running and responding!" -ForegroundColor Green
        $success = $true
        break
    }

    Write-Host "   Still waiting... ($elapsed/$Timeout seconds)" -ForegroundColor Gray
    Start-Sleep -Seconds $Delay
    $elapsed += $Delay
}

if ($success) {
    Write-Host ""
    Write-Host "🌐 Starting Angular frontend..." -ForegroundColor Yellow

    # Change to Angular directory and start the app in a new terminal
    $angularDir = Join-Path $PSScriptRoot ".." "src" "Angular"
    $angularArgs = @("serve", "--proxy-config", "proxy.conf.json")
    $angularProcess = Start-ProcessInTerminal -Command "ng" -Arguments $angularArgs -WorkingDirectory $angularDir

    if ($angularProcess) {
        Write-Host "✅ Angular process started with PID: $($angularProcess.Id)" -ForegroundColor Green
        Write-Host ""
        Write-Host "🎉 Both applications are now running!" -ForegroundColor Green
        Write-Host "   API: $ApiUrl" -ForegroundColor Cyan
        Write-Host "   Angular: http://localhost:4200" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "💡 To stop both applications, close their terminal windows or use Ctrl+C in each." -ForegroundColor Yellow
    } else {
        Write-Host "❌ Failed to start Angular process" -ForegroundColor Red
        Write-Host "💡 You can manually start Angular by running: cd $angularDir && ng serve --proxy-config proxy.conf.json" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ API did not respond within $Timeout seconds." -ForegroundColor Red
    Write-Host "👉 Check if the API failed to start, or is listening on a different URL/port." -ForegroundColor Yellow
    Write-Host "💡 You can manually check the API terminal for error messages." -ForegroundColor Yellow
    exit 1
}
