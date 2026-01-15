#!/usr/bin/env pwsh
# Enterprise DataSeeder CLI - PowerShell Script
# Usage: ./seeder.ps1 [command] [options]

param(
    [Parameter(Position = 0)]
    [string]$Command,
    
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

# Change to script directory
Set-Location $PSScriptRoot

if ([string]::IsNullOrEmpty($Command)) {
    Write-Host "Enterprise DataSeeder CLI" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: ./seeder.ps1 [command] [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Green
    Write-Host "  seed      - Seed the database with sample data"
    Write-Host "  clear     - Clear all data from the database"
    Write-Host "  migrate   - Apply pending database migrations"
    Write-Host "  reset     - Drop, recreate, and seed the database"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Green
    Write-Host "  ./seeder.ps1 seed"
    Write-Host "  ./seeder.ps1 seed --products 50000 --users 5000"
    Write-Host "  ./seeder.ps1 clear --confirm"
    Write-Host "  ./seeder.ps1 migrate"
    Write-Host "  ./seeder.ps1 reset --products 100"
    Write-Host ""
    Write-Host "For more help: ./seeder.ps1 help" -ForegroundColor Yellow
    exit 0
}

if ($Command -eq "help") {
    dotnet run --help
} else {
    $allArgs = @($Command) + $Arguments
    dotnet run @allArgs
}
