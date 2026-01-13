# Production Authentication System

## Overview

This implementation includes production-ready authentication with:

✅ **User Management** - Database-backed user authentication  
✅ **Password Hashing** - BCrypt password hashing with salt  
✅ **Account Lockout** - Protection against brute force attacks  
✅ **JWT Tokens** - Secure access tokens with claims  
✅ **Refresh Tokens** - Long-lived tokens for token renewal  
✅ **Role-Based Access** - User roles (Admin, Manager, User)  
✅ **IP Tracking** - Track login IPs for security auditing  

## Database Schema

### Entities Created

- **User** - User accounts with password hashes
- **Role** - User roles (Admin, Manager, User)
- **UserRole** - Many-to-many relationship
- **RefreshToken** - Refresh token management

## Security Features

### 1. Password Hashing

- Uses BCrypt with 12 rounds of salting
- Passwords are never stored in plain text
- Implemented via `IPasswordHasher` interface

### 2. Account Lockout

- **Max Failed Attempts:** 5
- **Lockout Duration:** 30 minutes
- Failed attempts reset on successful login
- Clear error messages for locked accounts

### 3. Token Security

- **Access Token:** 24 hours (configurable)
- **Refresh Token:** 7 days
- Tokens include user ID, roles, and email claims
- IP address tracked for all token operations

### 4. Refresh Token Rotation

- Old refresh tokens are revoked when used
- New refresh token generated on each refresh
- Tracks token replacement chain
- Can revoke all user tokens

## API Endpoints

### POST /api/auth/login

Authenticate user and receive tokens.

**Request:**

```json
{
  "username": "admin",
  "password": "Admin@123"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1Ni...",
  "refreshToken": "Abc123RefreshToken...",
  "tokenType": "Bearer",
  "expiresAt": "2026-01-13T12:00:00Z",
  "username": "admin",
  "email": "admin@enterprise.com",
  "firstName": "System",
  "lastName": "Administrator",
  "roles": ["Admin"]
}
```

### POST /api/auth/refresh

Refresh access token using refresh token.

**Request:**

```json
{
  "refreshToken": "Abc123RefreshToken..."
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1Ni...",
  "refreshToken": "NewRefreshToken...",
  "tokenType": "Bearer",
  "expiresAt": "2026-01-13T12:00:00Z"
}
```

### GET /api/auth/me

Test endpoint to verify authentication (requires Bearer token).

**Headers:**

```
Authorization: Bearer eyJhbGciOiJIUzI1Ni...
```

**Response:**

```json
{
  "username": "admin",
  "isAuthenticated": true,
  "claims": [
    { "type": "sub", "value": "admin" },
    { "type": "role", "value": "Admin" },
    { "type": "email", "value": "admin@enterprise.com" }
  ]
}
```

## Default Users

The system seeds three default users on first run:

### Admin User

- **Username:** `admin`
- **Password:** `Admin@123`
- **Role:** Admin
- **Email:** <admin@enterprise.com>

### Manager User

- **Username:** `manager`
- **Password:** `Manager@123`
- **Role:** Manager
- **Email:** <manager@enterprise.com>

### Regular User

- **Username:** `user`
- **Password:** `User@123`
- **Role:** User
- **Email:** <user@enterprise.com>

## Running the Application

### 1. Create Initial Migration

```bash
cd src/Enterprise.Infrastructure
dotnet ef migrations add AddAuthenticationTables --startup-project ../Enterprise.WebApi
```

### 2. Update Database

```bash
dotnet ef database update --startup-project ../Enterprise.WebApi
```

### 3. Run Application

```bash
cd ../Enterprise.WebApi
dotnet run
```

The database will be automatically seeded with default users on first run.

## Configuration

Edit `appsettings.json` to configure JWT settings:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration123456",
    "Issuer": "Enterprise",
    "Audience": "EnterpriseUsers",
    "ExpirationHours": 24
  }
}
```

**⚠️ IMPORTANT:** Change the `SecretKey` in production to a strong random value!

## Usage in Swagger

1. Navigate to `/swagger`
2. Click **POST /api/auth/login**
3. Try it out with credentials: `admin` / `Admin@123`
4. Copy the `token` from the response
5. Click **Authorize** button at the top
6. Enter: `Bearer {your-token}`
7. Now you can access protected endpoints

## Architecture

### Application Layer

- **Commands:** LoginCommand, RefreshTokenCommand
- **Handlers:** LoginCommandHandler, RefreshTokenCommandHandler
- **Interfaces:** IPasswordHasher

### Infrastructure Layer

- **Repositories:** UserRepository, RefreshTokenRepository
- **Services:** PasswordHasher (BCrypt implementation)
- **Seeding:** DatabaseSeeder

### Domain Layer

- **Entities:** User, Role, UserRole, RefreshToken
- **Interfaces:** IUserRepository, IRefreshTokenRepository

## Security Best Practices Implemented

✅ Passwords hashed with BCrypt (12 rounds)  
✅ Account lockout after 5 failed attempts  
✅ Refresh token rotation on use  
✅ IP address tracking for security auditing  
✅ Role-based authorization ready  
✅ User activation/deactivation support  
✅ Last login timestamp tracking  
✅ Token expiration configurable  
✅ Secure random token generation (64 bytes)  

## Future Enhancements

Consider adding:

- Email verification on registration
- Password reset via email
- Two-factor authentication (2FA)
- Password complexity requirements
- Password history to prevent reuse
- Session management and device tracking
- Rate limiting on login endpoint
- CAPTCHA after multiple failed attempts

## Testing

Test the authentication flow:

```bash
# Login
curl -X POST https://localhost:7235/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'

# Use token to access protected endpoint
curl -X GET https://localhost:7235/api/auth/me \
  -H "Authorization: Bearer {your-token}"

# Refresh token
curl -X POST https://localhost:7235/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"{your-refresh-token}"}'
```

## Error Handling

The system provides clear error messages:

- **401 Unauthorized:** Invalid credentials
- **401 Unauthorized:** Account locked (with time remaining)
- **401 Unauthorized:** Account disabled
- **401 Unauthorized:** Invalid refresh token
- **400 Bad Request:** Missing required fields

All errors use the global exception handler with RFC 7807 Problem Details format.
