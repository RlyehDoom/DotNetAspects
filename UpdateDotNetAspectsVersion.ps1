# PowerShell script to update DotNetAspects package version in all ICBanking projects
param(
    [string]$TargetPath = "C:\GIT\ICB7C\ICBanking",
    [string]$NewVersion = "1.3.5"
)

Write-Host "Searching for projects referencing DotNetAspects in: $TargetPath" -ForegroundColor Cyan
Write-Host "Target version: $NewVersion" -ForegroundColor Cyan
Write-Host ""

# Find all .csproj files
$csprojFiles = Get-ChildItem -Path $TargetPath -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue

$updatedCount = 0
$skippedCount = 0

foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw

    # Check if file contains DotNetAspects reference
    if ($content -match 'PackageReference\s+Include="DotNetAspects"') {
        # Get current version
        $currentVersionMatch = [regex]::Match($content, 'PackageReference\s+Include="DotNetAspects"\s+Version="([^"]+)"')
        $currentVersion = if ($currentVersionMatch.Success) { $currentVersionMatch.Groups[1].Value } else { "unknown" }

        if ($currentVersion -eq $NewVersion) {
            Write-Host "SKIP: $($file.FullName) - Already at version $NewVersion" -ForegroundColor Yellow
            $skippedCount++
        }
        else {
            # Update the version
            $newContent = $content -replace '(PackageReference\s+Include="DotNetAspects"\s+Version=")[^"]+"', "`${1}$NewVersion`""

            Set-Content -Path $file.FullName -Value $newContent -NoNewline
            Write-Host "UPDATED: $($file.FullName)" -ForegroundColor Green
            Write-Host "         $currentVersion -> $NewVersion" -ForegroundColor Gray
            $updatedCount++
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Updated: $updatedCount projects" -ForegroundColor Green
Write-Host "  Skipped: $skippedCount projects (already up to date)" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan
