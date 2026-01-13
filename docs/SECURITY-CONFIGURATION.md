# Security Configuration Guide

## JWT Secret Key Configuration

### ⚠️ Important: Never commit secrets to source control

The JWT secret key should be stored securely using one of the following methods:

## Development: User Secrets

User Secrets is a secure way to store sensitive data during development. The secrets are stored outside your project tree, so they won't accidentally get committed to source control.

### Setup User Secrets

1. **Initialize User Secrets** (from the WebApi project directory):

   ```bash
   cd src/EnterpriseApi.WebApi
   dotnet user-secrets init
   ```

2. **Set JWT Secret Key**:

   ```bash
   dotnet user-secrets set "JwtSettings:SecretKey" "YourStrongSecretKeyHere_Min32Characters!"
   dotnet user-secrets set "JwtSettings:Issuer" "EnterpriseAPI"
   dotnet user-secrets set "JwtSettings:Audience" "EnterpriseAPIUsers"
   dotnet user-secrets set "JwtSettings:ExpirationHours" "24"
   ```

3. **View stored secrets**:

   ```bash
   dotnet user-secrets list
   ```

4. **Remove the hardcoded secret** from `appsettings.json`:

   ```json
   "JwtSettings": {
     "SecretKey": "",  // Leave empty or remove - will use User Secrets
     "Issuer": "EnterpriseAPI",
     "Audience": "EnterpriseAPIUsers",
     "ExpirationHours": 24
   }
   ```

### How User Secrets Work

- Secrets are stored in: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json` (Windows)
- User Secrets automatically override appsettings.json values during development
- Only works in Development environment
- Each developer has their own secrets

## Production: Environment Variables

For production, use environment variables or a secure secret management system:

### Option 1: Environment Variables

```bash
export JwtSettings__SecretKey="YourProductionSecretKey"
export JwtSettings__Issuer="EnterpriseAPI"
export JwtSettings__Audience="EnterpriseAPIUsers"
```

Windows PowerShell:

```powershell
$env:JwtSettings__SecretKey="YourProductionSecretKey"
$env:JwtSettings__Issuer="EnterpriseAPI"
$env:JwtSettings__Audience="EnterpriseAPIUsers"
```

### Option 2: Azure Key Vault (Recommended for Azure)

1. **Install Azure Key Vault package**:

   ```bash
   dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
   ```

2. **Update Program.cs**:

   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri($"https://{keyVaultName}.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

3. **Store secrets in Azure Key Vault**:

   ```bash
   az keyvault secret set --vault-name <vault-name> --name JwtSettings--SecretKey --value "YourSecret"
   ```

### Option 3: AWS Secrets Manager

For AWS deployments, use AWS Secrets Manager with the AWS SDK.

### Option 4: Docker Secrets

When using Docker Compose or Kubernetes:

```yaml
# docker-compose.yml
services:
  api:
    environment:
      - JwtSettings__SecretKey=${JWT_SECRET}
    secrets:
      - jwt_secret

secrets:
  jwt_secret:
    external: true
```

## Security Best Practices

### JWT Secret Key Requirements

- ✅ Minimum 32 characters (256 bits)
- ✅ Use cryptographically secure random values
- ✅ Rotate keys periodically
- ✅ Different keys for each environment
- ❌ Never use predictable values
- ❌ Never commit to source control
- ❌ Never share between environments

### Generate a Secure Secret Key

**PowerShell**:

```powershell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | % {[char]$_})
```

**Linux/Mac**:

```bash
openssl rand -base64 64
```

**C# Console**:

```csharp
using System.Security.Cryptography;
var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
Console.WriteLine(key);
```

## Configuration Priority

ASP.NET Core loads configuration in this order (last wins):

1. appsettings.json
2. appsettings.{Environment}.json
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments

## Verification

Check that secrets are loaded correctly:

```csharp
// In Startup/Program.cs
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT Secret Key is not configured!");
}
```

## Connection Strings

Similarly, database connection strings should also be secured:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EnterpriseApiDb;User Id=sa;Password=YourPassword;"
```

## .gitignore

Ensure these patterns are in your `.gitignore`:

```
appsettings.*.json
**/appsettings.local.json
*.user
secrets.json
*.env
.env.*
```

## Troubleshooting

**Secrets not loading?**

- Verify User Secrets is initialized: Check for `<UserSecretsId>` in `.csproj`
- Check environment: User Secrets only work in Development
- View loaded configuration: Use `builder.Configuration.AsEnumerable()` to debug

**Production deployment fails?**

- Ensure environment variables are set
- Check configuration provider order
- Verify secret names match (use `__` for nested config)

## Related Documentation

- [ASP.NET Core User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration](https://docs.microsoft.com/azure/key-vault/)
- [Environment Variables in .NET](https://docs.microsoft.com/dotnet/core/tools/dotnet-environment-variables)
