# DataSeeder CLI Quick Reference

## Location

`src/Enterprise.DataSeeder/`

## Quick Commands

### Windows (PowerShell)

```powershell
cd src\Enterprise.DataSeeder
.\seeder.ps1 seed
.\seeder.ps1 seed --products 50000 --users 5000
.\seeder.ps1 clear --confirm
.\seeder.ps1 migrate
.\seeder.ps1 reset
```

### Windows (Command Prompt)

```cmd
cd src\Enterprise.DataSeeder
seeder.bat seed
seeder.bat seed --products 50000 --users 5000
seeder.bat clear --confirm
seeder.bat migrate
seeder.bat reset
```

### Linux/macOS

```bash
cd src/Enterprise.DataSeeder
chmod +x seeder.sh
./seeder.sh seed
./seeder.sh seed --products 50000 --users 5000
./seeder.sh clear --confirm
./seeder.sh migrate
./seeder.sh reset
```

### Direct .NET Command

```bash
cd src/Enterprise.DataSeeder
dotnet run -- seed
dotnet run -- seed --products 50000 --users 5000
dotnet run -- clear --confirm
dotnet run -- migrate
dotnet run -- reset
```

## Commands Overview

| Command | Description | Options |
|---------|-------------|---------|
| `seed` | Seed database with sample data | `--products` `--users` `--force` |
| `clear` | Clear all data (preserves schema) | `--confirm` (required) |
| `migrate` | Apply pending migrations | None |
| `reset` | Drop, recreate, and seed | `--products` `--users` |

## Default Values

- **Products**: 10,000
- **Users**: 1,000

## Default Seeded Users

| Username | Password | Role |
|----------|----------|------|
| admin | Admin@123 | Admin |
| manager | Manager@123 | Manager |
| user | User@123 | User |

## Common Workflows

### Initial Setup

```bash
dotnet run -- migrate
dotnet run -- seed
```

### Development Reset

```bash
dotnet run -- reset --products 1000 --users 100
```

### Load Testing

```bash
dotnet run -- reset --products 100000 --users 10000
```

### Clear and Re-seed

```bash
dotnet run -- clear --confirm
dotnet run -- seed --products 20000 --users 2000
```

## Exit Codes

- `0` - Success
- `1` - Error (check logs)

## Logs

- Console: Real-time output
- File: `logs/seeder-{date}.txt`

## Full Documentation

See [README.md](../src/Enterprise.DataSeeder/README.md) for detailed documentation.
