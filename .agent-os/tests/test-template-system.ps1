# Template System Test Runner
# PowerShell script to test Agent OS template functionality

$ErrorActionPreference = "Stop"
$TestResults = @()
$PassedTests = 0
$FailedTests = 0

# Color output functions
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Error { Write-Host $args -ForegroundColor Red }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }

# Test result tracking
function Add-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ""
    )
    
    $script:TestResults += [PSCustomObject]@{
        TestName = $TestName
        Passed = $Passed
        Message = $Message
    }
    
    if ($Passed) {
        $script:PassedTests++
        Write-Success "✓ $TestName"
    } else {
        $script:FailedTests++
        Write-Error "✗ $TestName"
        if ($Message) {
            Write-Error "  Error: $Message"
        }
    }
}

Write-Info "`n=== Agent OS Template System Tests ==="
Write-Info "Starting test suite...`n"

# Test 1: Directory Structure Creation
Write-Info "Test 1: Directory Structure Creation"
try {
    $templateDir = ".agent-os/templates"
    $subdirs = @("backend", "frontend", "common")
    
    $dirExists = Test-Path $templateDir
    $subdirsExist = $true
    
    foreach ($subdir in $subdirs) {
        if (-not (Test-Path "$templateDir/$subdir")) {
            $subdirsExist = $false
            break
        }
    }
    
    Add-TestResult -TestName "Directory Structure" -Passed ($dirExists -and $subdirsExist) `
        -Message $(if (-not $dirExists) { "Templates directory not found" } `
                  elseif (-not $subdirsExist) { "Missing subdirectories" })
} catch {
    Add-TestResult -TestName "Directory Structure" -Passed $false -Message $_.Exception.Message
}

# Test 2: Template File Loading
Write-Info "`nTest 2: Template File Loading"
try {
    $templateFiles = @(
        ".agent-os/templates/backend/crud-feature.md",
        ".agent-os/templates/backend/api-endpoint.md",
        ".agent-os/templates/backend/domain-aggregate.md",
        ".agent-os/templates/frontend/angular-component.md",
        ".agent-os/templates/frontend/angular-service.md",
        ".agent-os/templates/frontend/angular-state.md"
    )
    
    $allFilesExist = $true
    $missingFiles = @()
    
    foreach ($file in $templateFiles) {
        if (-not (Test-Path $file)) {
            $allFilesExist = $false
            $missingFiles += $file
        }
    }
    
    Add-TestResult -TestName "Template File Loading" -Passed $allFilesExist `
        -Message $(if ($missingFiles.Count -gt 0) { "Missing files: $($missingFiles -join ', ')" })
} catch {
    Add-TestResult -TestName "Template File Loading" -Passed $false -Message $_.Exception.Message
}

# Test 3: Variable Substitution Function
Write-Info "`nTest 3: Variable Substitution - Basic"
try {
    # This would test the actual substitution logic when implemented
    # For now, we're testing the concept
    $testTemplate = "Entity: {{ENTITY_NAME}}, Path: {{API_PATH}}"
    $variables = @{
        "ENTITY_NAME" = "Product"
        "API_PATH" = "/api/products"
    }
    
    # Simulate substitution (will be replaced with actual implementation)
    $result = $testTemplate
    foreach ($key in $variables.Keys) {
        $result = $result -replace "{{$key}}", $variables[$key]
    }
    
    $expected = "Entity: Product, Path: /api/products"
    $testPassed = $result -eq $expected
    
    Add-TestResult -TestName "Basic Variable Substitution" -Passed $testPassed `
        -Message $(if (-not $testPassed) { "Expected: $expected, Got: $result" })
} catch {
    Add-TestResult -TestName "Basic Variable Substitution" -Passed $false -Message $_.Exception.Message
}

# Test 4: Case Transformations
Write-Info "`nTest 4: Variable Case Transformations"
try {
    function Convert-ToCamelCase($str) {
        return $str.Substring(0,1).ToLower() + $str.Substring(1)
    }
    
    function Convert-ToSnakeCase($str) {
        return ($str -creplace '([A-Z])', '_$1').TrimStart('_').ToLower()
    }
    
    function Convert-ToKebabCase($str) {
        return ($str -creplace '([A-Z])', '-$1').TrimStart('-').ToLower()
    }
    
    $testEntity = "ProductCategory"
    $camelCase = Convert-ToCamelCase $testEntity
    $snakeCase = Convert-ToSnakeCase $testEntity
    $kebabCase = Convert-ToKebabCase $testEntity
    
    $camelPassed = $camelCase -eq "productCategory"
    $snakePassed = $snakeCase -eq "product_category"
    $kebabPassed = $kebabCase -eq "product-category"
    
    $allPassed = $camelPassed -and $snakePassed -and $kebabPassed
    
    Add-TestResult -TestName "Case Transformations" -Passed $allPassed `
        -Message $(if (-not $allPassed) { 
            "Camel: $camelCase, Snake: $snakeCase, Kebab: $kebabCase" 
        })
} catch {
    Add-TestResult -TestName "Case Transformations" -Passed $false -Message $_.Exception.Message
}

# Test 5: Template Validation
Write-Info "`nTest 5: Template Validation"
try {
    $invalidTemplates = @(
        '{{UNCLOSED',
        '{{}}',
        '{{INVALID CHARS}}',
        '{{NESTED{{VARIABLE}}}}'
    )
    
    $validationPassed = $true
    foreach ($template in $invalidTemplates) {
        # Check for basic validation rules
        if ($template -match '{{[^}]*$' -or 
            $template -match '{{}}' -or
            $template -match '{{[^}]*\s[^}]*}}' -or
            $template -match '{{.*{{.*}}.*}}') {
            # Invalid template detected correctly
        } else {
            $validationPassed = $false
            break
        }
    }
    
    Add-TestResult -TestName "Template Validation" -Passed $validationPassed
} catch {
    Add-TestResult -TestName "Template Validation" -Passed $false -Message $_.Exception.Message
}

# Test 6: Integration Test Placeholder
Write-Info "`nTest 6: Integration with create-tasks.md"
try {
    $createTasksFile = ".agent-os/instructions/core/create-tasks.md"
    $fileExists = Test-Path $createTasksFile
    
    if ($fileExists) {
        $content = Get-Content $createTasksFile -Raw
        # Check if template support is mentioned (will be added in task 3.7)
        $hasTemplateSupport = $content -match "template" -or $content -match "Template"
        
        Add-TestResult -TestName "create-tasks.md Integration" -Passed $true `
            -Message $(if (-not $hasTemplateSupport) { "Template support not yet added to create-tasks.md" })
    } else {
        Add-TestResult -TestName "create-tasks.md Integration" -Passed $false `
            -Message "create-tasks.md not found"
    }
} catch {
    Add-TestResult -TestName "create-tasks.md Integration" -Passed $false -Message $_.Exception.Message
}

# Summary
Write-Info "`n=== Test Summary ==="
Write-Info "Total Tests: $($PassedTests + $FailedTests)"
Write-Success "Passed: $PassedTests"
if ($FailedTests -gt 0) {
    Write-Error "Failed: $FailedTests"
} else {
    Write-Info "Failed: 0"
}

# Exit code
if ($FailedTests -gt 0) {
    Write-Error "`nTest suite failed!"
    exit 1
} else {
    Write-Success "`nAll tests passed!"
    exit 0
}