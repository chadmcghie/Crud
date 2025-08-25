param(
    [string]$ApiProject = "..\Backend\Api\Api.csproj",
    [string]$ApiUrl = "http://localhost:5071/",
    [int]$Timeout = 60,
    [int]$Delay = 2
)

Write-Host "Starting API from $ApiProject ..."

# Start API as a background job (keeps script alive)
$apiJob = Start-Job { dotnet run --project "..\Backend\Api\Api.csproj" --launch-profile http }

$elapsed = 0
$success = $false

Write-Host "Waiting for API at $ApiUrl ..."

while ($elapsed -lt $Timeout) {
    try {
        $response = Invoke-WebRequest -Uri $ApiUrl -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ API is running."
            $success = $true
            break
        }
    } catch {
        # still waiting
    }

    Start-Sleep -Seconds $Delay
    $elapsed += $Delay
}

if ($success) {
    Write-Host "Starting Angular frontend..."
    ng serve --proxy-config proxy.conf.json
} else {
    Write-Host "❌ API did not respond within $Timeout seconds."
    Stop-Job $apiJob | Out-Null
    Remove-Job $apiJob | Out-Null
    exit 1
}
