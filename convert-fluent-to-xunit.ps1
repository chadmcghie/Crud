# PowerShell script to convert FluentAssertions to xUnit Assert
param(
    [Parameter(Mandatory=$true)]
    [string]$FilePath
)

$content = Get-Content $FilePath -Raw

# Common replacements
$content = $content -replace '\.Should\(\)\.Be\(([^)]+)\);', 'Assert.Equal($1, '
$content = $content -replace '\.Should\(\)\.NotBeNull\(\);', 'Assert.NotNull('
$content = $content -replace '\.Should\(\)\.BeNull\(\);', 'Assert.NotNull('
$content = $content -replace '\.Should\(\)\.BeEmpty\(\);', 'Assert.Empty('
$content = $content -replace '\.Should\(\)\.NotBeEmpty\(\);', 'Assert.NotEmpty('
$content = $content -replace '\.Should\(\)\.HaveCount\(([^)]+)\);', 'Assert.Equal($1, '
$content = $content -replace '\.Should\(\)\.Contain\(([^)]+)\);', 'Assert.Contains($1, '
$content = $content -replace '\.Should\(\)\.BeTrue\(\);', 'Assert.True('
$content = $content -replace '\.Should\(\)\.BeFalse\(\);', 'Assert.False('
$content = $content -replace '\.Should\(\)\.BeGreaterThan\(([^)]+)\);', 'Assert.True( > $1);'
$content = $content -replace '\.Should\(\)\.BeGreaterOrEqualTo\(([^)]+)\);', 'Assert.True( >= $1);'

Set-Content $FilePath $content
