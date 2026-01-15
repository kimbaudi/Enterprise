# Commands CLI - Quick Start Guide

## 🚀 Get Started in 3 Steps

### Step 1: Navigate to Commands

```bash
cd src/Enterprise.Commands
```

### Step 2: Run Your First Command

```bash
dotnet run -- seed
```

This will:

- ✅ Create database (if it doesn't exist)
- ✅ Apply migrations
- ✅ Create 3 default users (admin, manager, user)
- ✅ Seed 10,000 products
- ✅ Seed 1,000 additional users

### Step 3: Start the API

```bash
cd ../Enterprise.WebApi
dotnet run
```

Visit `https://localhost:5001` and login with:

- Username: `admin`
- Password: `Admin@123`

---

## 🎯 Common Scenarios

### Development Setup (Small Dataset)

```bash
dotnet run -- seed --products 1000 --users 100
```

Perfect for local development and testing.

### Load Testing (Large Dataset)

```bash
dotnet run -- reset --products 1000 --users 100
```

Great for performance testing and benchmarking.

### Fresh Start

```bash
dotnet run -- reset
```

Drops everything and creates a clean database.

### CI/CD Pipeline

```bash
# Apply schema updates
dotnet run -- migrate

# Seed minimal test data
dotnet run -- seed --products 100 --users 10
```

---

## 📋 All Commands at a Glance

| Command | What It Does | When to Use |
|---------|--------------|-------------|
| `seed` | Add sample data | First-time setup, need more data |
| `clear` | Delete all data | Clean slate, keep schema |
| `migrate` | Update schema | After creating migrations |
| `reset` | Drop & recreate | Fresh start, change schema |

---

## 🛡️ Safety First

### Destructive Operations Require Confirmation

```bash
# ❌ This will NOT work (missing --confirm)
dotnet run -- clear

# ✅ This works
dotnet run -- clear --confirm
```

### Force Seeding (Overwrite Existing Data)

```bash
# ❌ Won't seed if data exists
dotnet run -- seed

# ✅ Seeds even if data exists
dotnet run -- seed --force
```

---

## 💡 Pro Tips

### 1. Use Helper Scripts

**Windows (PowerShell):**

```powershell
.\commands.ps1 seed --products 5000
```

**Linux/macOS:**

```bash
chmod +x commands.sh
./commands.sh seed --products 5000
```

### 2. Check Logs

Logs are saved to `logs/commands-{date}.txt`

### 3. Get Help Anytime

```bash
dotnet run -- --help           # All commands
dotnet run -- seed --help      # Seed options
dotnet run -- reset --help     # Reset options
```

### 4. Quick Version Check

```bash
dotnet run -- --version
```

---

## 🎓 Learning Path

### Beginner

```bash
# 1. Create database and seed default data
dotnet run -- seed

# 2. View the data in Swagger UI
cd ../Enterprise.WebApi && dotnet run
# Open https://localhost:5001
```

### Intermediate

```bash
# 1. Reset with custom counts
dotnet run -- reset --products 5000 --users 500

# 2. Clear specific data
dotnet run -- clear --confirm

# 3. Re-seed with different counts
dotnet run -- seed --products 2000 --users 200
```

### Advanced

```bash
# 1. Migrate only (no seeding)
dotnet run -- migrate

# 2. Force seed over existing data
dotnet run -- seed --force --products 50000 --users 5000

# 3. Automate in scripts
./commands.ps1 reset && ./commands.ps1 seed --products 100
```

---

## ⚡ Performance Guide

| Dataset Size | Estimated Time | Use Case |
|--------------|----------------|----------|
| 1k products, 100 users | ~5-10 seconds | Quick tests |
| 10k products, 1k users | ~30-60 seconds | Default development |
| 50k products, 5k users | ~2-5 minutes | Load testing |
| 100k products, 10k users | ~5-10 minutes | Stress testing |

*Times vary based on hardware and SQL Server configuration*

---

## 🔍 Troubleshooting

### "Command not found"

Make sure you're in the correct directory:

```bash
cd src/Enterprise.Commands
```

### "Cannot connect to database"

Check your connection string in `appsettings.json`

### "Migrations failed"

Run migrations from the solution root:

```bash
cd ../..
dotnet ef database update --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
```

---

## 📚 More Resources

- **Full Documentation**: [README.md](README.md)
- **Quick Reference**: [../../docs/COMMANDS-CLI.md](../../docs/COMMANDS-CLI.md)
- **Implementation Details**: [../../docs/COMMANDS-IMPLEMENTATION.md](../../docs/COMMANDS-IMPLEMENTATION.md)

---

## ✅ What's Next?

After seeding:

1. ✨ Start the API: `cd ../Enterprise.WebApi && dotnet run`
2. 🔐 Login with test credentials (admin/Admin@123)
3. 📊 Test endpoints in Swagger UI
4. 🧪 Run integration tests: `dotnet test` from solution root
5. 🚀 Deploy to your environment

**Happy Coding!** 🎉
