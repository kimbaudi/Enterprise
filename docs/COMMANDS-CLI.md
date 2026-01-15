# Commands CLI Quick Reference

## Location

`src/Enterprise.Commands/`

## Quick Commands

### Windows (PowerShell)

```powershell
cd src\Enterprise.Commands
.\commands.ps1 seed
.\commands.ps1 seed --products 50000 --users 5000
.\commands.ps1 clear --confirm
.\commands.ps1 migrate
.\commands.ps1 reset
```

### Windows (Command Prompt)

```cmd
cd src\Enterprise.Commands
commands.bat seed
commands.bat seed --products 50000 --users 5000
commands.bat clear --confirm
commands.bat migrate
commands.bat reset
```

### Linux/macOS

```bash
cd src/Enterprise.Commands
chmod +x commands.sh
./commands.sh seed
./commands.sh seed --products 50000 --users 5000
./commands.sh clear --confirm
./commands.sh migrate
./commands.sh reset
```

### Direct .NET Command

```bash
cd src/Enterprise.Commands
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

- **Products**: 100
- **Users**: 10

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
dotnet run -- reset --products 1000 --users 100
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
- File: `logs/commands-{date}.txt`

## Full Documentation

See [README.md](../src/Enterprise.Commands/README.md) for detailed documentation.
