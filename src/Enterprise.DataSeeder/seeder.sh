#!/bin/bash
# Enterprise DataSeeder CLI - Bash Script
# Usage: ./seeder.sh [command] [options]

# Change to script directory
cd "$(dirname "$0")"

if [ -z "$1" ]; then
    echo -e "\033[1;36mEnterprise DataSeeder CLI\033[0m"
    echo -e "\033[1;36m=========================\033[0m"
    echo ""
    echo -e "\033[1;33mUsage: ./seeder.sh [command] [options]\033[0m"
    echo ""
    echo -e "\033[1;32mCommands:\033[0m"
    echo "  seed      - Seed the database with sample data"
    echo "  clear     - Clear all data from the database"
    echo "  migrate   - Apply pending database migrations"
    echo "  reset     - Drop, recreate, and seed the database"
    echo ""
    echo -e "\033[1;32mExamples:\033[0m"
    echo "  ./seeder.sh seed"
    echo "  ./seeder.sh seed --products 50000 --users 5000"
    echo "  ./seeder.sh clear --confirm"
    echo "  ./seeder.sh migrate"
    echo "  ./seeder.sh reset --products 100"
    echo ""
    echo -e "\033[1;33mFor more help: ./seeder.sh help\033[0m"
    exit 0
fi

if [ "$1" = "help" ]; then
    dotnet run --help
else
    dotnet run "$@"
fi
