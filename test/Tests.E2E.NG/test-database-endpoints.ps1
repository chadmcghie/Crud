# PowerShell script to test the database management endpoints
# This script verifies that the database reset and seed functionality works correctly

param(
    [string]$ApiUrl = "http://localhost:5172",
    [int]$WorkerIndex = 0
)

Write-Host "🧪 Testing Database Management Endpoints" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host "Worker Index: $WorkerIndex" -ForegroundColor Gray
Write-Host ""

# Function to make HTTP requests with error handling
function Invoke-ApiRequest {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null
    )
    
    try {
        $headers = @{ "Content-Type" = "application/json" }
        
        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Body $jsonBody -Headers $headers
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $headers
        }
        
        return @{ Success = $true; Data = $response }
    }
    catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Test 1: Check API Health
Write-Host "1️⃣  Testing API Health..." -ForegroundColor Yellow
$healthResult = Invoke-ApiRequest -Url "$ApiUrl/health"

if ($healthResult.Success) {
    Write-Host "   ✅ API is healthy" -ForegroundColor Green
} else {
    Write-Host "   ❌ API health check failed: $($healthResult.Error)" -ForegroundColor Red
    Write-Host "   💡 Make sure the API is running with: dotnet run --environment=Testing" -ForegroundColor Yellow
    exit 1
}

# Test 2: Check Database Status
Write-Host "2️⃣  Testing Database Status..." -ForegroundColor Yellow
$statusResult = Invoke-ApiRequest -Url "$ApiUrl/api/database/status"

if ($statusResult.Success) {
    Write-Host "   ✅ Database status retrieved successfully" -ForegroundColor Green
    Write-Host "   📊 Environment: $($statusResult.Data.environment)" -ForegroundColor Gray
    Write-Host "   📊 Can Connect: $($statusResult.Data.canConnect)" -ForegroundColor Gray
    Write-Host "   📊 People Count: $($statusResult.Data.peopleCount)" -ForegroundColor Gray
    Write-Host "   📊 Roles Count: $($statusResult.Data.rolesCount)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Database status failed: $($statusResult.Error)" -ForegroundColor Red
}

# Test 3: Reset Database
Write-Host "3️⃣  Testing Database Reset..." -ForegroundColor Yellow
$resetBody = @{
    workerIndex = $WorkerIndex
    preserveSchema = $true
}

$resetResult = Invoke-ApiRequest -Url "$ApiUrl/api/database/reset" -Method "POST" -Body $resetBody

if ($resetResult.Success) {
    Write-Host "   ✅ Database reset successful" -ForegroundColor Green
    Write-Host "   📝 Message: $($resetResult.Data.message)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Database reset failed: $($resetResult.Error)" -ForegroundColor Red
}

# Test 4: Seed Database
Write-Host "4️⃣  Testing Database Seed..." -ForegroundColor Yellow
$seedBody = @{
    workerIndex = $WorkerIndex
}

$seedResult = Invoke-ApiRequest -Url "$ApiUrl/api/database/seed" -Method "POST" -Body $seedBody

if ($seedResult.Success) {
    Write-Host "   ✅ Database seed successful" -ForegroundColor Green
    Write-Host "   📝 Message: $($seedResult.Data.message)" -ForegroundColor Gray
} else {
    Write-Host "   ❌ Database seed failed: $($seedResult.Error)" -ForegroundColor Red
}

# Test 5: Verify Seed Data
Write-Host "5️⃣  Verifying Seed Data..." -ForegroundColor Yellow
$statusAfterSeed = Invoke-ApiRequest -Url "$ApiUrl/api/database/status"

if ($statusAfterSeed.Success) {
    Write-Host "   ✅ Database status after seed retrieved" -ForegroundColor Green
    Write-Host "   📊 People Count: $($statusAfterSeed.Data.peopleCount)" -ForegroundColor Gray
    Write-Host "   📊 Roles Count: $($statusAfterSeed.Data.rolesCount)" -ForegroundColor Gray
    
    if ($statusAfterSeed.Data.rolesCount -gt 0) {
        Write-Host "   ✅ Seed data appears to be present" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  No seed data found after seeding" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ Could not verify seed data: $($statusAfterSeed.Error)" -ForegroundColor Red
}

# Test 6: Test Reset Again (should clear seed data)
Write-Host "6️⃣  Testing Reset After Seed..." -ForegroundColor Yellow
$resetAgainResult = Invoke-ApiRequest -Url "$ApiUrl/api/database/reset" -Method "POST" -Body $resetBody

if ($resetAgainResult.Success) {
    Write-Host "   ✅ Second database reset successful" -ForegroundColor Green
    
    # Check if data was cleared
    $statusAfterReset = Invoke-ApiRequest -Url "$ApiUrl/api/database/status"
    if ($statusAfterReset.Success) {
        Write-Host "   📊 People Count After Reset: $($statusAfterReset.Data.peopleCount)" -ForegroundColor Gray
        Write-Host "   📊 Roles Count After Reset: $($statusAfterReset.Data.rolesCount)" -ForegroundColor Gray
        
        if ($statusAfterReset.Data.peopleCount -eq 0 -and $statusAfterReset.Data.rolesCount -eq 0) {
            Write-Host "   ✅ Database successfully cleared by reset" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Some data remains after reset" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "   ❌ Second database reset failed: $($resetAgainResult.Error)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Database endpoint testing complete!" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Run E2E tests with: npm run test:api-only" -ForegroundColor Gray
Write-Host "   2. Try parallel tests with: npm run test:parallel" -ForegroundColor Gray
Write-Host "   3. Check the PARALLEL-TESTING-GUIDE.md for more details" -ForegroundColor Gray
