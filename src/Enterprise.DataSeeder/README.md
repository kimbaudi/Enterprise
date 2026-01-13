# Enterprise DataSeeder CLI

A command-line utility for managing database seeding and migrations for the Enterprise Web API.

## Features

- **Seed**: Populate database with sample data (products and users)
- **Clear**: Remove all data from the database
- **Migrate**: Apply pending database migrations
- **Reset**: Drop, recreate, and seed the database with fresh data

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Configured connection string in `appsettings.json`

## Installation

Navigate to the DataSeeder directory:

```bash
cd src/Enterprise.DataSeeder
```

Restore dependencies:

```bash
dotnet restore
```

## Usage

### Seed Command

Seed the database with sample data:

```bash
# Default: 10,000 products and 1,000 users
dotnet run seed

# Custom counts
dotnet run seed --products 50000 --users 5000

# Force seeding even if data exists
dotnet run seed --products 1000 --users 100 --force
```

**Options:**

- `--products <number>`: Number of products to seed (default: 10000)
- `--users <number>`: Number of users to seed (default: 1000)
- `--force`: Force seeding even if data already exists

**Default Users Created:**

- Username: `admin` / Password: `Admin@123` (Admin role)
- Username: `manager` / Password: `Manager@123` (Manager role)
- Username: `user` / Password: `User@123` (User role)

### Clear Command

Remove all data from the database (preserves schema and roles):

```bash
# Requires confirmation flag
dotnet run clear --confirm
```

**Options:**

- `--confirm`: Required flag to confirm data deletion

**Warning:** This will delete all products, users, and refresh tokens. Roles are preserved.

### Migrate Command

Apply pending database migrations:

```bash
dotnet run migrate
```

This ensures your database schema is up-to-date with the latest migrations.

### Reset Command

Drop the entire database, recreate it, and seed with fresh data:

```bash
# Default: 10,000 products and 1,000 users
dotnet run reset

# Custom counts
dotnet run reset --products 5000 --users 500
```

**Options:**

- `--products <number>`: Number of products to seed (default: 10000)
- `--users <number>`: Number of users to seed (default: 1000)

**Warning:** This completely destroys and recreates the database. All data will be lost!

## Help

View available commands and options:

```bash
dotnet run --help
dotnet run seed --help
dotnet run clear --help
dotnet run migrate --help
dotnet run reset --help
```

## Configuration

Edit `appsettings.json` to configure:

- **Connection String**: Database connection settings
- **JWT Settings**: Token generation configuration
- **Logging**: Log levels and output configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnterpriseDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

## Logging

Logs are written to:

- **Console**: Real-time output
- **File**: `logs/seeder-{date}.txt` (rolling daily)

## Examples

### Quick Start - Development Setup

```bash
# Create database, apply migrations, and seed with default data
dotnet run seed
```

### Load Testing Setup

```bash
# Seed large dataset for performance testing
dotnet run seed --products 100000 --users 10000
```

### Clean Slate

```bash
# Complete database reset
dotnet run reset
```

### Production-Like Dataset

```bash
# Reset and seed with production-sized data
dotnet run reset --products 50000 --users 5000
```

### Clear and Re-seed

```bash
# Clear existing data
dotnet run clear --confirm

# Seed fresh data
dotnet run seed --products 20000 --users 2000
```

## Exit Codes

- `0`: Success
- `1`: Error occurred (check logs for details)

## Troubleshooting

### Connection Issues

If you encounter connection errors:

1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure database user has appropriate permissions

### Migration Issues

If migrations fail:

```bash
# From solution root, create a fresh migration
dotnet ef migrations add InitialCreate --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi

# Apply migrations manually
dotnet ef database update --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
```

### Performance

Seeding large datasets can be slow. Expected times:

- 10,000 products + 1,000 users: ~30-60 seconds
- 50,000 products + 5,000 users: ~2-5 minutes
- 100,000 products + 10,000 users: ~5-10 minutes

Times vary based on hardware and SQL Server configuration.

## Integration with CI/CD

Use in automated pipelines:

```bash
# GitHub Actions / Azure DevOps example
cd src/Enterprise.DataSeeder
dotnet run migrate  # Ensure schema is current
dotnet run seed --products 1000 --users 100  # Minimal test data
```

## Development

The seeder uses:

- **System.CommandLine**: CLI parsing and command handling
- **Serilog**: Structured logging
- **EF Core**: Database operations
- **DI Container**: Service resolution from main application

To extend functionality, modify [Program.cs](Program.cs) and add new commands following the existing pattern.
