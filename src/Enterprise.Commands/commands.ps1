#!/usr/bin/env pwsh
# Enterprise Commands CLI - PowerShell Script
# Usage: ./commands.ps1 [command] [options]

param(
    [Parameter(Position = 0)]
    [string]$Command,
    
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Arguments
)

# Change to script directory
Set-Location $PSScriptRoot

if ([string]::IsNullOrEmpty($Command)) {
    Write-Host "Enterprise Commands CLI" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: ./commands.ps1 [command] [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Green
    Write-Host "  seed      - Seed the database with sample data"
    Write-Host "  clear     - Clear all data from the database"
    Write-Host "  migrate   - Apply pending database migrations"
    Write-Host "  reset     - Drop, recreate, and seed the database"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Green
    Write-Host "  ./commands.ps1 seed"
    Write-Host "  ./commands.ps1 seed --products 50000 --users 5000"
    Write-Host "  ./commands.ps1 clear --confirm"
    Write-Host "  ./commands.ps1 migrate"
    Write-Host "  ./commands.ps1 reset --products 100"
    Write-Host ""
    Write-Host "For more help: ./commands.ps1 help" -ForegroundColor Yellow
    exit 0
}

if ($Command -eq "help") {
    dotnet run --help
} else {
    $allArgs = @($Command) + $Arguments
    dotnet run @allArgs
}
