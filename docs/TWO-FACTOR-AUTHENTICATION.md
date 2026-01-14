# Two-Factor Authentication (2FA) Implementation

## Overview

This document describes the Two-Factor Authentication (2FA) implementation using Time-based One-Time Password (TOTP) for the Enterprise Web API. The implementation follows the CQRS pattern with Clean Architecture principles.

## Features

- **TOTP-based 2FA**: Uses industry-standard TOTP algorithm (RFC 6238)
- **QR Code Setup**: Easy setup via QR code scanning with authenticator apps
- **Recovery Codes**: 8 one-time use recovery codes for account recovery
- **Secure Storage**: Secrets and recovery codes are stored securely
- **Flexible Login**: Supports both TOTP codes and recovery codes during login

## Architecture

### Domain Layer

**User Entity** ([src/Enterprise.Domain/Entities/User.cs](src/Enterprise.Domain/Entities/User.cs))

- `TwoFactorEnabled`: Boolean flag indicating if 2FA is active
- `TwoFactorSecret`: Base32-encoded TOTP secret key (nullable)
- `RecoveryCodes`: JSON array of hashed recovery codes (nullable)

### Application Layer

#### Commands

1. **Enable2FA** ([Features/Auth/Commands/Enable2FA](src/Enterprise.Application/Features/Auth/Commands/Enable2FA))
   - Generates a new TOTP secret
   - Returns QR code URL and manual entry key
   - Does NOT enable 2FA until verified

2. **Verify2FA** ([Features/Auth/Commands/Verify2FA](src/Enterprise.Application/Features/Auth/Commands/Verify2FA))
   - Validates the TOTP code during setup
   - Generates 8 recovery codes (hashed and stored)
   - Enables 2FA on successful verification

3. **Validate2FA** ([Features/Auth/Commands/Validate2FA](src/Enterprise.Application/Features/Auth/Commands/Validate2FA))
   - Validates TOTP code or recovery code during login
   - Issues JWT tokens on successful validation
   - Consumes recovery codes (single-use)

4. **Disable2FA** ([Features/Auth/Commands/Disable2FA](src/Enterprise.Application/Features/Auth/Commands/Disable2FA))
   - Requires valid TOTP code for security
   - Clears all 2FA data (secret, recovery codes)

#### Queries

1. **Get2FAStatus** ([Features/Auth/Queries/Get2FAStatus](src/Enterprise.Application/Features/Auth/Queries/Get2FAStatus))
   - Returns current 2FA status
   - Indicates if recovery codes are available

#### Services

**ITwoFactorService** ([Common/Interfaces/ITwoFactorService.cs](src/Enterprise.Application/Common/Interfaces/ITwoFactorService.cs))

- `GenerateSecret()`: Creates random TOTP secret
- `GenerateQrCodeUrl()`: Formats otpauth:// URL for QR codes
- `ValidateCode()`: Validates TOTP codes with time tolerance
- `GenerateRecoveryCodes()`: Creates 8 random recovery codes
- `HashRecoveryCode()` / `VerifyRecoveryCode()`: SHA256 hashing for secure storage

### Infrastructure Layer

**TwoFactorService** ([Services/TwoFactorService.cs](src/Enterprise.Infrastructure/Services/TwoFactorService.cs))

- Implements `ITwoFactorService` using Otp.NET library
- Uses 160-bit secrets (20 bytes)
- 30-second time step with ±1 step tolerance
- Recovery codes: 8 characters (XXXX-XXXX format)

### API Endpoints

#### POST `/api/v1/auth/2fa/enable`

**Authorization**: Required (Bearer token)

Initiates 2FA setup by generating a secret and QR code.

**Response**:

```json
{
  "data": {
    "secret": "JBSWY3DPEHPK3PXP",
    "qrCodeUrl": "otpauth://totp/Enterprise:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Enterprise",
    "manualEntryKey": "JBSW Y3DP EHPK 3PXP"
  },
  "success": true,
  "message": "Request processed successfully"
}
```

#### POST `/api/v1/auth/2fa/verify`

**Authorization**: Required (Bearer token)

Verifies setup by validating the first TOTP code and enables 2FA.

**Request**:

```json
{
  "code": "123456"
}
```

**Response**:

```json
{
  "data": {
    "isVerified": true,
    "recoveryCodes": [
      "ABCD-EFGH",
      "IJKL-MNOP",
      "..."
    ]
  },
  "success": true
}
```

⚠️ **Important**: Save recovery codes securely. They are shown only once!

#### POST `/api/v1/auth/2fa/validate`

**Authorization**: Not required

Completes login by validating 2FA code after successful username/password authentication.

**Request**:

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "code": "123456"
}
```

**Response**:

```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "...",
    "tokenType": "Bearer",
    "expiresAt": "2026-01-14T14:00:00Z",
    "username": "john.doe",
    "email": "john@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["User"]
  },
  "success": true
}
```

#### POST `/api/v1/auth/2fa/disable`

**Authorization**: Required (Bearer token)

Disables 2FA after validating current TOTP code.

**Request**:

```json
{
  "code": "123456"
}
```

**Response**:

```json
{
  "data": true,
  "success": true
}
```

#### GET `/api/v1/auth/2fa/status`

**Authorization**: Required (Bearer token)

Returns current 2FA status for the authenticated user.

**Response**:

```json
{
  "data": {
    "isEnabled": true,
    "hasRecoveryCodes": true
  },
  "success": true
}
```

## Login Flow with 2FA

### Standard Login (2FA Disabled)

```
1. POST /api/v1/auth/login
   → Returns JWT token immediately
```

### Login with 2FA Enabled

```
1. POST /api/v1/auth/login
   → Returns { requiresTwoFactor: true, twoFactorUserId: "..." }
   
2. POST /api/v1/auth/2fa/validate
   → Validates TOTP/recovery code
   → Returns JWT token on success
```

### Modified LoginResponse

```csharp
public class LoginResponse
{
    // ... existing properties ...
    public bool RequiresTwoFactor { get; set; }
    public Guid? TwoFactorUserId { get; set; }
}
```

When `RequiresTwoFactor = true`, the client must:

1. Prompt user for 2FA code
2. Call `/api/v1/auth/2fa/validate` with `TwoFactorUserId` and code
3. Receive final JWT token

## Setup Instructions

### For Users

1. **Enable 2FA**:
   - Call `POST /api/v1/auth/2fa/enable`
   - Scan QR code with authenticator app (Google Authenticator, Authy, Microsoft Authenticator)
   - Or manually enter the key

2. **Verify Setup**:
   - Enter the 6-digit code from authenticator app
   - Call `POST /api/v1/auth/2fa/verify`
   - **Save the 8 recovery codes securely**

3. **Login**:
   - Enter username/password as usual
   - When prompted, enter 6-digit TOTP code or recovery code
   - Call `POST /api/v1/auth/2fa/validate`

4. **Recovery**:
   - If device is lost, use one of the recovery codes
   - Each recovery code can be used only once
   - Disable and re-enable 2FA to generate new codes

### For Developers

#### Testing 2FA Locally

1. **Install an Authenticator App** on your phone:
   - Google Authenticator (iOS/Android)
   - Microsoft Authenticator (iOS/Android)
   - Authy (iOS/Android/Desktop)

2. **Enable 2FA via Swagger**:

   ```bash
   cd src/Enterprise.WebApi
   dotnet run
   # Navigate to https://localhost:5001
   ```

   - Login with default user: `admin` / `Admin@123`
   - Click "Authorize" and paste JWT token
   - Call `POST /api/v1/auth/2fa/enable`
   - Scan the QR code URL with your authenticator app
   - Call `POST /api/v1/auth/2fa/verify` with the 6-digit code

3. **Test Login Flow**:
   - Logout (clear token)
   - Call `POST /api/v1/auth/login` with credentials
   - Note `requiresTwoFactor: true` and `twoFactorUserId`
   - Call `POST /api/v1/auth/2fa/validate` with code

## Security Considerations

### Secrets Storage

- TOTP secrets are stored in Base32 encoding
- Recovery codes are hashed with SHA256 before storage
- Secrets are cleared when 2FA is disabled

### Code Validation

- TOTP validation includes ±1 time step (±30 seconds tolerance)
- Recovery codes are single-use and removed after validation
- Both TOTP and recovery codes accepted during login

### Rate Limiting

- 2FA endpoints inherit auth controller rate limits
- Failed validation attempts count toward login limits
- Account lockout applies after 5 failed attempts

### Best Practices

1. **Always save recovery codes** during setup
2. **Store recovery codes securely** (password manager, printed copy)
3. **Regenerate recovery codes** if compromised
4. **Require TOTP code** to disable 2FA (prevent unauthorized disable)
5. **Educate users** about backup codes before enabling

## Database Schema

### Migration: `AddTwoFactorAuthentication`

Added to `Users` table:

```sql
ALTER TABLE Users ADD TwoFactorEnabled bit NOT NULL DEFAULT 0;
ALTER TABLE Users ADD TwoFactorSecret nvarchar(max) NULL;
ALTER TABLE Users ADD RecoveryCodes nvarchar(max) NULL;
```

To apply migration:

```bash
dotnet ef database update --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
```

## Dependencies

- **Otp.NET** (1.4.1): TOTP generation and validation
- **QRCoder** (1.7.0): QR code generation (future enhancement for image generation)

## Future Enhancements

1. **QR Code Image Generation**: Return base64-encoded QR code image
2. **Backup Methods**: SMS, Email backup codes
3. **Trusted Devices**: Remember device for 30 days
4. **2FA Recovery Flow**: Reset 2FA via email verification
5. **Admin Override**: Allow admins to disable 2FA for locked-out users
6. **Audit Logging**: Log all 2FA events (enable, disable, validation)
7. **WebAuthn/FIDO2**: Hardware key support

## Troubleshooting

### "Invalid verification code" during setup

- Ensure phone time is synchronized (NTP)
- TOTP is time-based; clock skew causes failures
- Try generating a new code (refreshes every 30 seconds)

### Recovery codes not working

- Check for typos (format: XXXX-XXXX)
- Remove hyphens and try again
- Each code works only once

### Lost device and recovery codes

- Contact administrator to disable 2FA
- Future: Implement self-service recovery via email

### QR code not scanning

- Ensure QR code URL is properly formatted
- Use manual entry key instead
- Some apps require "otpauth://" scheme

## Testing

Unit tests for 2FA components can be added to:

```
tests/Enterprise.Application.Tests/Features/Auth/Commands/
  - Enable2FACommandHandlerTests.cs
  - Verify2FACommandHandlerTests.cs
  - Validate2FACommandHandlerTests.cs
  - Disable2FACommandHandlerTests.cs
```

Example test structure:

```csharp
[Fact]
public async Task Handle_ValidCode_EnablesTwoFactor()
{
    // Arrange
    var user = CreateTestUser();
    _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
        .ReturnsAsync(user);
    _twoFactorServiceMock.Setup(x => x.ValidateCode(It.IsAny<string>(), "123456"))
        .Returns(true);
    
    // Act
    var result = await _handler.Handle(
        new Verify2FACommand(user.Id, "123456"), 
        default);
    
    // Assert
    result.IsVerified.Should().BeTrue();
    result.RecoveryCodes.Should().HaveCount(8);
}
```

## Summary

Two-Factor Authentication is now fully implemented with:

- ✅ TOTP-based authentication using Otp.NET
- ✅ QR code setup for easy configuration
- ✅ Recovery codes for backup access
- ✅ Modified login flow with 2FA check
- ✅ Complete CQRS command/query handlers
- ✅ RESTful API endpoints with Swagger documentation
- ✅ Database migration applied
- ✅ Secure secret and recovery code storage

Users can now enhance their account security with industry-standard two-factor authentication!
