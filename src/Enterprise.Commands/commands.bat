@echo off
REM Enterprise Commands CLI - Windows Batch Script
REM Usage: commands.bat [command] [options]

cd /d "%~dp0"

if "%1"=="" (
    echo Usage: commands.bat [command] [options]
    echo.
    echo Commands:
    echo   seed      - Seed the database with sample data
    echo   clear     - Clear all data from the database
    echo   migrate   - Apply pending database migrations
    echo   reset     - Drop, recreate, and seed the database
    echo.
    echo Examples:
    echo   commands.bat seed
    echo   commands.bat seed --products 50000 --users 5000
    echo   commands.bat clear --confirm
    echo   commands.bat migrate
    echo   commands.bat reset --products 100
    echo.
    echo For more help: commands.bat help
    exit /b 0
)

if "%1"=="help" (
    dotnet run --help
) else (
    dotnet run %*
)
