#!/bin/bash
# Enterprise Commands CLI - Bash Script
# Usage: ./commands.sh [command] [options]

# Change to script directory
cd "$(dirname "$0")"

if [ -z "$1" ]; then
    echo -e "\033[1;36mEnterprise Commands CLI\033[0m"
    echo -e "\033[1;36m=========================\033[0m"
    echo ""
    echo -e "\033[1;33mUsage: ./commands.sh [command] [options]\033[0m"
    echo ""
    echo -e "\033[1;32mCommands:\033[0m"
    echo "  seed      - Seed the database with sample data"
    echo "  clear     - Clear all data from the database"
    echo "  migrate   - Apply pending database migrations"
    echo "  reset     - Drop, recreate, and seed the database"
    echo ""
    echo -e "\033[1;32mExamples:\033[0m"
    echo "  ./commands.sh seed"
    echo "  ./commands.sh seed --products 50000 --users 5000"
    echo "  ./commands.sh clear --confirm"
    echo "  ./commands.sh migrate"
    echo "  ./commands.sh reset --products 100"
    echo ""
    echo -e "\033[1;33mFor more help: ./commands.sh help\033[0m"
    exit 0
fi

if [ "$1" = "help" ]; then
    dotnet run --help
else
    dotnet run "$@"
fi
