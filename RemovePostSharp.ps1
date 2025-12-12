# PowerShell script to remove PostSharp package references from all projects
param(
    [string]$TargetPath = "path/to/your/solution/folder"  # Change this to your target path
)

Write-Host "Removing PostSharp references from: $TargetPath" -ForegroundColor Cyan
Write-Host ""

# Find all .csproj files
$csprojFiles = Get-ChildItem -Path $TargetPath -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue

$updatedCount = 0

foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Remove PostSharp PackageReference (various formats)
    $content = $content -replace '(?s)\s*<PackageReference\s+Include="PostSharp"[^/]*?/>\s*', "`n"
    $content = $content -replace '(?s)\s*<PackageReference\s+Include="PostSharp"[^>]*>.*?</PackageReference>\s*', "`n"

    # Remove PostSharp.Patterns.Diagnostics
    $content = $content -replace '(?s)\s*<PackageReference\s+Include="PostSharp\.Patterns\.Diagnostics"[^/]*?/>\s*', "`n"

    # Remove any other PostSharp.* packages
    $content = $content -replace '(?s)\s*<PackageReference\s+Include="PostSharp\.[^"]*"[^/]*?/>\s*', "`n"

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "CLEANED: $($file.FullName)" -ForegroundColor Green
        $updatedCount++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Removed PostSharp from $updatedCount projects" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
