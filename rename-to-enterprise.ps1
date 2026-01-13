# Rename EnterpriseApi to Enterprise - PowerShell Script
# Run this script after closing VS Code

Write-Host "Starting rename operation from EnterpriseApi to Enterprise..." -ForegroundColor Green

$rootPath = "C:\Users\Paul\Desktop\Enterprise"
Set-Location $rootPath

# Step 1: Rename project folders
Write-Host "`nStep 1: Renaming project folders..." -ForegroundColor Yellow

Rename-Item -Path "src\EnterpriseApi.Domain" -NewName "Enterprise.Domain"
Rename-Item -Path "src\EnterpriseApi.Application" -NewName "Enterprise.Application"
Rename-Item -Path "src\EnterpriseApi.Infrastructure" -NewName "Enterprise.Infrastructure"
Rename-Item -Path "src\EnterpriseApi.WebApi" -NewName "Enterprise.WebApi"
Rename-Item -Path "src\EnterpriseApi.DataSeeder" -NewName "Enterprise.DataSeeder"
Rename-Item -Path "tests\EnterpriseApi.Application.Tests" -NewName "Enterprise.Application.Tests"

Write-Host "Project folders renamed successfully" -ForegroundColor Green

# Step 2: Rename .csproj files
Write-Host "`nStep 2: Renaming .csproj files..." -ForegroundColor Yellow

Rename-Item -Path "src\Enterprise.Domain\EnterpriseApi.Domain.csproj" -NewName "Enterprise.Domain.csproj"
Rename-Item -Path "src\Enterprise.Application\EnterpriseApi.Application.csproj" -NewName "Enterprise.Application.csproj"
Rename-Item -Path "src\Enterprise.Infrastructure\EnterpriseApi.Infrastructure.csproj" -NewName "Enterprise.Infrastructure.csproj"
Rename-Item -Path "src\Enterprise.WebApi\EnterpriseApi.WebApi.csproj" -NewName "Enterprise.WebApi.csproj"
Rename-Item -Path "src\Enterprise.DataSeeder\EnterpriseApi.DataSeeder.csproj" -NewName "Enterprise.DataSeeder.csproj"
Rename-Item -Path "tests\Enterprise.Application.Tests\EnterpriseApi.Application.Tests.csproj" -NewName "Enterprise.Application.Tests.csproj"

Write-Host ".csproj files renamed successfully" -ForegroundColor Green

# Step 3: Update solution file
Write-Host "`nStep 3: Updating solution file..." -ForegroundColor Yellow

$solutionFile = "Enterprise.sln"
$solutionContent = Get-Content $solutionFile -Raw
$solutionContent = $solutionContent -replace 'EnterpriseApi\.Domain', 'Enterprise.Domain'
$solutionContent = $solutionContent -replace 'EnterpriseApi\.Application', 'Enterprise.Application'
$solutionContent = $solutionContent -replace 'EnterpriseApi\.Infrastructure', 'Enterprise.Infrastructure'
$solutionContent = $solutionContent -replace 'EnterpriseApi\.WebApi', 'Enterprise.WebApi'
$solutionContent = $solutionContent -replace 'EnterpriseApi\.DataSeeder', 'Enterprise.DataSeeder'
$solutionContent | Set-Content $solutionFile -NoNewline

Write-Host "Solution file updated successfully" -ForegroundColor Green

# Step 4: Update .csproj project references
Write-Host "`nStep 4: Updating project references in .csproj files..." -ForegroundColor Yellow

$csprojFiles = @(
    "src\Enterprise.Domain\Enterprise.Domain.csproj",
    "src\Enterprise.Application\Enterprise.Application.csproj",
    "src\Enterprise.Infrastructure\Enterprise.Infrastructure.csproj",
    "src\Enterprise.WebApi\Enterprise.WebApi.csproj",
    "src\Enterprise.DataSeeder\Enterprise.DataSeeder.csproj",
    "tests\Enterprise.Application.Tests\Enterprise.Application.Tests.csproj"
)

foreach ($csproj in $csprojFiles) {
    $content = Get-Content $csproj -Raw
    $content = $content -replace 'EnterpriseApi\.Domain', 'Enterprise.Domain'
    $content = $content -replace 'EnterpriseApi\.Application', 'Enterprise.Application'
    $content = $content -replace 'EnterpriseApi\.Infrastructure', 'Enterprise.Infrastructure'
    $content = $content -replace 'EnterpriseApi\.WebApi', 'Enterprise.WebApi'
    $content = $content -replace 'EnterpriseApi\.DataSeeder', 'Enterprise.DataSeeder'
    $content | Set-Content $csproj -NoNewline
}

Write-Host "Project references updated successfully" -ForegroundColor Green

# Step 5: Update C# files - namespaces and using statements
Write-Host "`nStep 5: Updating namespaces in C# files..." -ForegroundColor Yellow

$csFiles = Get-ChildItem -Path @("src", "tests") -Recurse -Filter "*.cs" -File

$totalFiles = $csFiles.Count
$currentFile = 0

foreach ($file in $csFiles) {
    $currentFile++
    Write-Progress -Activity "Updating C# files" -Status "$currentFile of $totalFiles" -PercentComplete (($currentFile / $totalFiles) * 100)
    
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'EnterpriseApi') {
        $content = $content -replace 'namespace EnterpriseApi\.', 'namespace Enterprise.'
        $content = $content -replace 'using EnterpriseApi\.', 'using Enterprise.'
        $content | Set-Content $file.FullName -NoNewline
    }
}

Write-Progress -Activity "Updating C# files" -Completed

Write-Host "Namespaces updated successfully in $totalFiles files" -ForegroundColor Green

# Step 6: Update other configuration files
Write-Host "`nStep 6: Updating configuration files..." -ForegroundColor Yellow

# Update WebApi.csproj.user if exists
$userFile = "src\Enterprise.WebApi\Enterprise.WebApi.csproj.user"
if (Test-Path $userFile) {
    $content = Get-Content $userFile -Raw
    $content = $content -replace 'EnterpriseApi\.WebApi', 'Enterprise.WebApi'
    $content | Set-Content $userFile -NoNewline
}

# Update HTTP file
$httpFile = "src\Enterprise.WebApi\EnterpriseApi.WebApi.http"
if (Test-Path $httpFile) {
    Rename-Item -Path $httpFile -NewName "Enterprise.WebApi.http"
}

Write-Host "Configuration files updated successfully" -ForegroundColor Green

# Final message
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Rename operation completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nAll projects have been renamed from EnterpriseApi.* to Enterprise.*" -ForegroundColor White
Write-Host "You can now reopen the workspace in VS Code and build the solution." -ForegroundColor White
Write-Host "`nRun: dotnet build" -ForegroundColor Yellow
