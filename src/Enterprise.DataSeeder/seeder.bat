@echo off
REM Enterprise DataSeeder CLI - Windows Batch Script
REM Usage: seeder.bat [command] [options]

cd /d "%~dp0"

if "%1"=="" (
    echo Usage: seeder.bat [command] [options]
    echo.
    echo Commands:
    echo   seed      - Seed the database with sample data
    echo   clear     - Clear all data from the database
    echo   migrate   - Apply pending database migrations
    echo   reset     - Drop, recreate, and seed the database
    echo.
    echo Examples:
    echo   seeder.bat seed
    echo   seeder.bat seed --products 50000 --users 5000
    echo   seeder.bat clear --confirm
    echo   seeder.bat migrate
    echo   seeder.bat reset --products 10000
    echo.
    echo For more help: seeder.bat help
    exit /b 0
)

if "%1"=="help" (
    dotnet run --help
) else (
    dotnet run %*
)
