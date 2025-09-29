# Agent OS Template Engine
# PowerShell implementation for template variable substitution

param(
    [Parameter(Mandatory=$true)]
    [string]$TemplatePath,
    
    [Parameter(Mandatory=$true)]
    [hashtable]$Variables,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ""
)

# Case transformation functions
function Convert-ToPascalCase {
    param([string]$text)
    return ($text -split '[-_\s]' | ForEach-Object { 
        $_.Substring(0,1).ToUpper() + $_.Substring(1).ToLower() 
    }) -join ''
}

function Convert-ToCamelCase {
    param([string]$text)
    $pascal = Convert-ToPascalCase $text
    return $pascal.Substring(0,1).ToLower() + $pascal.Substring(1)
}

function Convert-ToSnakeCase {
    param([string]$text)
    $text = $text -creplace '([A-Z])([A-Z][a-z])', '$1_$2'
    $text = $text -creplace '([a-z\d])([A-Z])', '$1_$2'
    $text = $text -replace '[-\s]', '_'
    return $text.ToLower()
}

function Convert-ToKebabCase {
    param([string]$text)
    $text = $text -creplace '([A-Z])([A-Z][a-z])', '$1-$2'
    $text = $text -creplace '([a-z\d])([A-Z])', '$1-$2'
    $text = $text -replace '[_\s]', '-'
    return $text.ToLower()
}

function Convert-ToUpperCase {
    param([string]$text)
    return $text.ToUpper()
}

function Convert-ToLowerCase {
    param([string]$text)
    return $text.ToLower()
}

# Main template processing function
function Process-Template {
    param(
        [string]$Template,
        [hashtable]$Vars
    )
    
    $result = $Template
    
    # Find all variable placeholders
    $pattern = '{{([^}:]+)(?::([^}]+))?}}'
    $matches = [regex]::Matches($Template, $pattern)
    
    foreach ($match in $matches) {
        $fullMatch = $match.Value
        $varName = $match.Groups[1].Value.Trim()
        $transform = $match.Groups[2].Value.Trim()
        
        if ($Vars.ContainsKey($varName)) {
            $value = $Vars[$varName]
            
            # Apply transformation if specified
            switch ($transform) {
                'PascalCase' { $value = Convert-ToPascalCase $value }
                'pascalCase' { $value = Convert-ToPascalCase $value }
                'camelCase' { $value = Convert-ToCamelCase $value }
                'snake_case' { $value = Convert-ToSnakeCase $value }
                'kebab-case' { $value = Convert-ToKebabCase $value }
                'UPPERCASE' { $value = Convert-ToUpperCase $value }
                'lowercase' { $value = Convert-ToLowerCase $value }
                default { 
                    if ($transform) {
                        Write-Warning "Unknown transformation: $transform"
                    }
                }
            }
            
            $result = $result.Replace($fullMatch, $value)
        } else {
            Write-Warning "Variable not found: $varName"
        }
    }
    
    return $result
}

# Validate template
function Test-Template {
    param([string]$Template)
    
    $errors = @()
    
    # Check for unclosed placeholders
    if ($Template -match '{{[^}]*$') {
        $errors += "Unclosed placeholder detected"
    }
    
    # Check for empty placeholders
    if ($Template -match '{{}}') {
        $errors += "Empty placeholder detected"
    }
    
    # Check for nested placeholders
    if ($Template -match '{{[^}]*{{') {
        $errors += "Nested placeholders are not supported"
    }
    
    return $errors
}

# Main execution
try {
    # Check if template file exists
    if (-not (Test-Path $TemplatePath)) {
        throw "Template file not found: $TemplatePath"
    }
    
    # Read template content
    $templateContent = Get-Content $TemplatePath -Raw
    
    # Validate template
    $validationErrors = Test-Template $templateContent
    if ($validationErrors.Count -gt 0) {
        throw "Template validation failed: $($validationErrors -join '; ')"
    }
    
    # Process template
    $processedContent = Process-Template -Template $templateContent -Vars $Variables
    
    # Output result
    if ($OutputPath) {
        # Create directory if it doesn't exist
        $outputDir = Split-Path $OutputPath -Parent
        if ($outputDir -and -not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
        }
        
        # Write to file
        $processedContent | Set-Content $OutputPath -Encoding UTF8
        Write-Host "Template processed and saved to: $OutputPath" -ForegroundColor Green
    } else {
        # Output to console
        Write-Output $processedContent
    }
    
    exit 0
} catch {
    Write-Error "Error processing template: $_"
    exit 1
}