# Two-Factor Authentication - Quick Reference

## Quick Start

### Enable 2FA for a User

1. **Login** to get JWT token:

```bash
POST /api/v1/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```

1. **Enable 2FA**:

```bash
POST /api/v1/auth/2fa/enable
Authorization: Bearer {your-token}
```

Response includes `qrCodeUrl` - scan with authenticator app.

1. **Verify with code**:

```bash
POST /api/v1/auth/2fa/verify
Authorization: Bearer {your-token}
{
  "code": "123456"
}
```

**Save the recovery codes returned!**

### Login with 2FA

1. **Initial login**:

```bash
POST /api/v1/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}
```

Response: `{ "requiresTwoFactor": true, "twoFactorUserId": "..." }`

1. **Complete login with 2FA**:

```bash
POST /api/v1/auth/2fa/validate
{
  "userId": "{twoFactorUserId-from-step-1}",
  "code": "123456"
}
```

## API Endpoints Summary

| Endpoint | Auth | Description |
|----------|------|-------------|
| `POST /api/v1/auth/2fa/enable` | ✅ Required | Start 2FA setup, get QR code |
| `POST /api/v1/auth/2fa/verify` | ✅ Required | Verify setup, enable 2FA, get recovery codes |
| `POST /api/v1/auth/2fa/validate` | ❌ Not Required | Complete login with 2FA code |
| `POST /api/v1/auth/2fa/disable` | ✅ Required | Disable 2FA with code |
| `GET /api/v1/auth/2fa/status` | ✅ Required | Check 2FA status |

## Code Examples

### C# Client

```csharp
// Enable 2FA
var enableResponse = await httpClient.PostAsync(
    "/api/v1/auth/2fa/enable", 
    null);
var enableData = await enableResponse.Content.ReadFromJsonAsync<ApiResponse<Enable2FAResponse>>();

// Show QR code: enableData.Data.QrCodeUrl
// Or show manual key: enableData.Data.ManualEntryKey

// Verify with authenticator code
var verifyRequest = new { code = "123456" };
var verifyResponse = await httpClient.PostAsJsonAsync(
    "/api/v1/auth/2fa/verify", 
    verifyRequest);
var verifyData = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<Verify2FAResponse>>();

// Save recovery codes: verifyData.Data.RecoveryCodes

// Login with 2FA
var loginResponse = await httpClient.PostAsJsonAsync(
    "/api/v1/auth/login", 
    new { username = "admin", password = "Admin@123" });
var loginData = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

if (loginData.Data.RequiresTwoFactor)
{
    // Prompt for 2FA code
    var validateRequest = new 
    { 
        userId = loginData.Data.TwoFactorUserId, 
        code = userEnteredCode 
    };
    var validateResponse = await httpClient.PostAsJsonAsync(
        "/api/v1/auth/2fa/validate", 
        validateRequest);
    var validateData = await validateResponse.Content.ReadFromJsonAsync<ApiResponse<Validate2FAResponse>>();
    
    // Use validateData.Data.Token
}
```

### JavaScript/TypeScript

```typescript
// Enable 2FA
const enableResponse = await fetch('/api/v1/auth/2fa/enable', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
});
const enableData = await enableResponse.json();

// Display QR code: enableData.data.qrCodeUrl
// Or manual key: enableData.data.manualEntryKey

// Verify setup
const verifyResponse = await fetch('/api/v1/auth/2fa/verify', {
  method: 'POST',
  headers: { 
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({ code: '123456' })
});
const verifyData = await verifyResponse.json();

// Save recovery codes: verifyData.data.recoveryCodes

// Login flow
const loginResponse = await fetch('/api/v1/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ username: 'admin', password: 'Admin@123' })
});
const loginData = await loginResponse.json();

if (loginData.data.requiresTwoFactor) {
  // Prompt user for 2FA code
  const code = prompt('Enter 2FA code:');
  
  const validateResponse = await fetch('/api/v1/auth/2fa/validate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ 
      userId: loginData.data.twoFactorUserId, 
      code 
    })
  });
  const validateData = await validateResponse.json();
  
  // Use validateData.data.token
}
```

## Authenticator Apps

Compatible with any TOTP authenticator:

- **Google Authenticator** (iOS, Android)
- **Microsoft Authenticator** (iOS, Android)
- **Authy** (iOS, Android, Desktop)
- **1Password** (iOS, Android, Desktop)
- **Bitwarden** (iOS, Android, Desktop)

## Recovery Codes

- **8 codes** generated during setup
- **Single-use**: Each code works only once
- **Format**: `XXXX-XXXX` (e.g., `A3B7-9K2M`)
- **Storage**: Save securely (password manager or printed)
- **Regeneration**: Disable and re-enable 2FA to get new codes

## Common Issues

**Problem**: "Invalid verification code"

- **Solution**: Check device time is synchronized, wait for new code (30s refresh)

**Problem**: QR code won't scan

- **Solution**: Use manual entry key instead

**Problem**: Lost phone and recovery codes

- **Solution**: Contact admin to disable 2FA on your account

**Problem**: Recovery code not working

- **Solution**: Remove hyphens, ensure it hasn't been used before

## Testing Locally

```bash
# 1. Start the API
cd src/Enterprise.WebApi
dotnet run

# 2. Open Swagger
https://localhost:5001

# 3. Login with default admin
POST /api/v1/auth/login
{
  "username": "admin",
  "password": "Admin@123"
}

# 4. Authorize with token (click "Authorize" button)

# 5. Enable 2FA
POST /api/v1/auth/2fa/enable

# 6. Scan QR code with authenticator app

# 7. Verify with code from app
POST /api/v1/auth/2fa/verify
{
  "code": "123456"
}

# 8. Test login flow (logout first)
POST /api/v1/auth/login
# Note requiresTwoFactor = true

POST /api/v1/auth/2fa/validate
# Use code from app
```

## Architecture Components

### Commands

- `Enable2FACommand` - Generate secret and QR code
- `Verify2FACommand` - Validate and activate 2FA
- `Validate2FACommand` - Validate during login
- `Disable2FACommand` - Turn off 2FA

### Services

- `ITwoFactorService` - TOTP operations interface
- `TwoFactorService` - Implementation using Otp.NET

### Database

- `User.TwoFactorEnabled` - Boolean flag
- `User.TwoFactorSecret` - Base32 secret (nullable)
- `User.RecoveryCodes` - JSON array of hashes (nullable)

## Security Features

✅ Industry-standard TOTP (RFC 6238)
✅ Secure secret generation (160-bit)
✅ SHA256-hashed recovery codes
✅ Time tolerance (±30 seconds)
✅ Single-use recovery codes
✅ Requires code to disable 2FA
✅ Clear secrets on disable

## Next Steps

1. **Users**: Enable 2FA in account settings for enhanced security
2. **Developers**: Integrate 2FA flow in frontend applications
3. **Admins**: Consider making 2FA mandatory for privileged accounts
4. **Operations**: Set up monitoring for 2FA events

For complete documentation, see [TWO-FACTOR-AUTHENTICATION.md](TWO-FACTOR-AUTHENTICATION.md)
