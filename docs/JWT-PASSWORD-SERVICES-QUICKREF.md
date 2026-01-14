# JWT & Password Services - Quick Reference

## IJwtTokenService - New Methods

### Token Validation

#### Validate Token Signature & Claims

```csharp
bool isValid = _jwtTokenService.ValidateToken(token);
if (!isValid)
{
    throw new UnauthorizedAccessException("Invalid token");
}
```

#### Check Token Expiration

```csharp
if (_jwtTokenService.IsTokenExpired(token))
{
    // Redirect to login or refresh token
    return Unauthorized("Token has expired");
}
```

---

### Token Parsing & Claim Extraction

#### Get ClaimsPrincipal

```csharp
var principal = _jwtTokenService.GetPrincipalFromToken(token);
if (principal != null)
{
    var claims = principal.Claims;
    // Use principal for authorization checks
}
```

#### Extract User ID

```csharp
var userId = _jwtTokenService.GetUserIdFromToken(token);
if (userId.HasValue)
{
    var user = await _userRepository.GetByIdAsync(userId.Value, ct);
}
```

#### Extract Username

```csharp
var username = _jwtTokenService.GetUsernameFromToken(token);
_logger.LogInformation("Request from user: {Username}", username);
```

#### Extract User Roles

```csharp
var roles = _jwtTokenService.GetRolesFromToken(token);
if (roles.Contains("Admin"))
{
    // Grant admin access
}
```

#### Get All Claims

```csharp
var claims = _jwtTokenService.GetClaimsFromToken(token);
foreach (var claim in claims)
{
    Console.WriteLine($"{claim.Type}: {claim.Value}");
}
```

---

### Token Utilities

#### Get Expiration Date

```csharp
var expiresAt = _jwtTokenService.GetTokenExpirationDate(token);
if (expiresAt.HasValue)
{
    Console.WriteLine($"Token expires at: {expiresAt.Value:yyyy-MM-dd HH:mm:ss}");
}
```

#### Get Remaining Lifetime

```csharp
var remaining = _jwtTokenService.GetRemainingTokenLifetime(token);
if (remaining < TimeSpan.FromMinutes(5))
{
    _logger.LogWarning("Token expiring soon: {Minutes} minutes remaining", remaining.TotalMinutes);
    // Consider refreshing token proactively
}
```

---

## IPasswordHasher - New Methods

### Password Validation

#### Validate Password Strength (Detailed)

```csharp
if (!_passwordHasher.ValidatePasswordStrength(password, out var errors))
{
    return BadRequest(new 
    { 
        Message = "Password does not meet requirements",
        Errors = errors 
    });
}
// Password is strong, proceed with registration
```

**Returns errors like:**

- "Password must be at least 8 characters long"
- "Password must contain at least one uppercase letter"
- "Password must contain at least one lowercase letter"
- "Password must contain at least one digit"
- "Password must contain at least one special character"
- "Password contains common weak patterns"

#### Quick Requirements Check

```csharp
if (_passwordHasher.MeetsMinimumRequirements(password))
{
    // Basic requirements met (length, uppercase, lowercase, digit)
    var hash = _passwordHasher.HashPassword(password);
}
```

---

### Password Generation

#### Generate Random Password

```csharp
// Generate 12-character password (default)
var tempPassword = _passwordHasher.GenerateRandomPassword();

// Generate custom length password (min 8)
var longPassword = _passwordHasher.GenerateRandomPassword(16);

// Example output: "xK9$mP2wQ!vL"
```

**Use Cases:**

- Temporary passwords for new users
- Password reset flows
- Admin-created accounts

#### Generate Secure Token

```csharp
// Generate 32-byte URL-safe token (default)
var resetToken = _passwordHasher.GenerateSecureToken();

// Generate custom length token
var verificationToken = _passwordHasher.GenerateSecureToken(64);

// Example output: "kL9x3mP2wQ-vL4hJ8nB_5tY6rE1zA"
```

**Use Cases:**

- Password reset tokens
- Email verification tokens
- API keys
- Session identifiers

---

### Hash Utilities

#### Check if Hash Needs Rehashing

```csharp
if (_passwordHasher.VerifyPassword(password, user.PasswordHash))
{
    // Login successful
    
    if (_passwordHasher.NeedsRehash(user.PasswordHash))
    {
        // Old hash detected, rehash with stronger cost factor
        user.PasswordHash = _passwordHasher.HashPassword(password);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Password rehashed for user {UserId}", user.Id);
    }
}
```

---

## Common Usage Patterns

### Token Refresh Logic

```csharp
public async Task<RefreshTokenResponse> RefreshTokenAsync(string expiredToken, string refreshToken)
{
    // Validate expired token structure (don't validate lifetime)
    var userId = _jwtTokenService.GetUserIdFromToken(expiredToken);
    if (!userId.HasValue)
        throw new UnauthorizedAccessException("Invalid token");
    
    // Validate refresh token
    var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, default);
    if (storedToken == null || storedToken.UserId != userId.Value)
        throw new UnauthorizedAccessException("Invalid refresh token");
    
    // Generate new tokens
    var user = await _userRepository.GetByIdWithRolesAsync(userId.Value, default);
    var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
    
    return new RefreshTokenResponse { AccessToken = newAccessToken };
}
```

### Password Registration Validator

```csharp
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    private readonly IPasswordHasher _passwordHasher;
    
    public RegisterCommandValidator(IPasswordHasher passwordHasher)
    {
        _passwordHasher = passwordHasher;
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .Must(BeStrongPassword)
            .WithMessage("Password does not meet strength requirements");
    }
    
    private bool BeStrongPassword(string password)
    {
        return _passwordHasher.ValidatePasswordStrength(password, out _);
    }
}
```

### Custom Authorization Attribute

```csharp
public class TokenValidationAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var jwtService = context.HttpContext.RequestServices
            .GetRequiredService<IJwtTokenService>();
        
        var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            if (!jwtService.ValidateToken(token))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
            
            var remaining = jwtService.GetRemainingTokenLifetime(token);
            if (remaining < TimeSpan.FromMinutes(5))
            {
                context.HttpContext.Response.Headers.Add(
                    "X-Token-Expiring-Soon", 
                    "true");
            }
        }
        
        base.OnActionExecuting(context);
    }
}
```

### Password Reset Flow

```csharp
// Request password reset
public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email)
{
    var user = await _userRepository.GetByEmailAsync(email, default);
    if (user == null)
        return new ForgotPasswordResponse { Success = true }; // Don't reveal user existence
    
    // Generate secure token
    var resetToken = _passwordHasher.GenerateSecureToken(32);
    user.PasswordResetToken = resetToken;
    user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
    
    await _unitOfWork.SaveChangesAsync(default);
    await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName, resetToken, default);
    
    return new ForgotPasswordResponse { Success = true };
}

// Reset password
public async Task<ResetPasswordResponse> ResetPasswordAsync(string email, string token, string newPassword)
{
    var user = await _userRepository.GetByEmailAsync(email, default);
    if (user == null || !user.HasValidPasswordResetToken(token))
        throw new InvalidOperationException("Invalid or expired reset token");
    
    // Validate new password strength
    if (!_passwordHasher.ValidatePasswordStrength(newPassword, out var errors))
        throw new ValidationException("Password", errors);
    
    // Hash and save new password
    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
    user.PasswordResetToken = null;
    user.PasswordResetTokenExpiry = null;
    
    await _unitOfWork.SaveChangesAsync(default);
    await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FirstName, default);
    
    return new ResetPasswordResponse { Success = true };
}
```

### Temporary Password Creation

```csharp
public async Task<CreateUserResponse> CreateUserWithTemporaryPasswordAsync(CreateUserCommand command)
{
    // Generate temporary password
    var tempPassword = _passwordHasher.GenerateRandomPassword(12);
    
    var user = new User
    {
        Username = command.Username,
        Email = command.Email,
        FirstName = command.FirstName,
        LastName = command.LastName,
        PasswordHash = _passwordHasher.HashPassword(tempPassword),
        IsActive = true
    };
    
    await _userRepository.AddAsync(user, default);
    await _unitOfWork.SaveChangesAsync(default);
    
    // Email temporary password
    await _emailService.SendEmailAsync(
        user.Email,
        user.FirstName,
        "Your Account Credentials",
        $"<p>Your temporary password is: <strong>{tempPassword}</strong></p><p>Please change it on first login.</p>",
        default);
    
    return new CreateUserResponse 
    { 
        UserId = user.Id,
        TemporaryPassword = tempPassword // Only for admin view
    };
}
```

### Token Expiration Middleware

```csharp
public class TokenExpirationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IJwtTokenService _jwtService;
    
    public TokenExpirationMiddleware(RequestDelegate next, IJwtTokenService jwtService)
    {
        _next = next;
        _jwtService = jwtService;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"]
            .FirstOrDefault()?
            .Replace("Bearer ", "");
        
        if (!string.IsNullOrEmpty(token))
        {
            if (_jwtService.IsTokenExpired(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    Error = "Token expired",
                    Code = "TOKEN_EXPIRED"
                });
                return;
            }
            
            var remaining = _jwtService.GetRemainingTokenLifetime(token);
            if (remaining < TimeSpan.FromMinutes(10))
            {
                context.Response.Headers.Add("X-Token-Refresh-Recommended", "true");
                context.Response.Headers.Add("X-Token-Expires-In-Seconds", 
                    ((int)remaining.TotalSeconds).ToString());
            }
        }
        
        await _next(context);
    }
}
```

### Role-Based Access Check

```csharp
public async Task<bool> HasPermissionAsync(string token, string requiredRole)
{
    if (!_jwtService.ValidateToken(token))
        return false;
    
    var roles = _jwtService.GetRolesFromToken(token);
    return roles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase);
}

// Usage
if (!await HasPermissionAsync(token, "Admin"))
{
    return Forbid();
}
```

### Password Strength Meter API

```csharp
[HttpPost("password/check-strength")]
public ActionResult<PasswordStrengthResponse> CheckPasswordStrength([FromBody] CheckPasswordRequest request)
{
    var isValid = _passwordHasher.ValidatePasswordStrength(request.Password, out var errors);
    var meetsMin = _passwordHasher.MeetsMinimumRequirements(request.Password);
    
    // Calculate strength score
    int score = 0;
    if (request.Password.Length >= 8) score++;
    if (request.Password.Length >= 12) score++;
    if (Regex.IsMatch(request.Password, @"[A-Z]")) score++;
    if (Regex.IsMatch(request.Password, @"[a-z]")) score++;
    if (Regex.IsMatch(request.Password, @"[0-9]")) score++;
    if (Regex.IsMatch(request.Password, @"[^a-zA-Z0-9]")) score++;
    
    return Ok(new PasswordStrengthResponse
    {
        IsValid = isValid,
        MeetsMinimumRequirements = meetsMin,
        Score = score,
        Strength = score switch
        {
            <= 2 => "Weak",
            <= 4 => "Fair",
            <= 5 => "Good",
            _ => "Strong"
        },
        Errors = errors
    });
}
```

---

## Testing Examples

### JWT Service Tests

```csharp
[Fact]
public void ValidateToken_WithValidToken_ReturnsTrue()
{
    // Arrange
    var user = CreateTestUser();
    var token = _jwtService.GenerateAccessToken(user);
    
    // Act
    var isValid = _jwtService.ValidateToken(token);
    
    // Assert
    isValid.Should().BeTrue();
}

[Fact]
public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
{
    // Arrange
    var user = CreateTestUser();
    var token = _jwtService.GenerateAccessToken(user);
    
    // Act
    var userId = _jwtService.GetUserIdFromToken(token);
    
    // Assert
    userId.Should().Be(user.Id);
}

[Fact]
public void GetRemainingTokenLifetime_WithNewToken_ReturnsPositiveTimeSpan()
{
    // Arrange
    var user = CreateTestUser();
    var token = _jwtService.GenerateAccessToken(user);
    
    // Act
    var remaining = _jwtService.GetRemainingTokenLifetime(token);
    
    // Assert
    remaining.Should().BeGreaterThan(TimeSpan.Zero);
    remaining.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(24));
}
```

### Password Hasher Tests

```csharp
[Theory]
[InlineData("weak", false)]
[InlineData("WeakPass", false)]
[InlineData("Strong123!", true)]
[InlineData("MyP@ssw0rd", true)]
public void ValidatePasswordStrength_WithVariousPasswords_ReturnsExpectedResult(string password, bool expectedValid)
{
    // Act
    var isValid = _passwordHasher.ValidatePasswordStrength(password, out var errors);
    
    // Assert
    isValid.Should().Be(expectedValid);
    if (!expectedValid)
    {
        errors.Should().NotBeEmpty();
    }
}

[Fact]
public void GenerateRandomPassword_ReturnsValidPassword()
{
    // Act
    var password = _passwordHasher.GenerateRandomPassword(12);
    
    // Assert
    password.Should().HaveLength(12);
    _passwordHasher.ValidatePasswordStrength(password, out _).Should().BeTrue();
}

[Fact]
public void GenerateSecureToken_ReturnsUniqueTokens()
{
    // Act
    var token1 = _passwordHasher.GenerateSecureToken(32);
    var token2 = _passwordHasher.GenerateSecureToken(32);
    
    // Assert
    token1.Should().NotBe(token2);
    token1.Should().NotBeNullOrWhiteSpace();
}
```

---

## Performance Tips

### JWT Token Service

- Cache token validation results (short TTL) to reduce repeated validation overhead
- Use `GetPrincipalFromToken` once and extract multiple claims from the principal
- Consider using `IsTokenExpired` for quick checks before full validation
- Store frequently accessed claims (userId, roles) in memory cache

### Password Hasher

- Validate password strength on client-side first to reduce server load
- Cache generated passwords/tokens temporarily when bulk creating users
- Use `MeetsMinimumRequirements` for quick checks, full validation only when needed
- Consider async password hashing for high-load scenarios

---

## Security Best Practices

✅ **Always validate tokens** before trusting claims  
✅ **Never log passwords or tokens** in plain text  
✅ **Use ValidatePasswordStrength** during registration and password changes  
✅ **Implement token refresh** before expiration (proactive refresh)  
✅ **Hash passwords immediately** upon receipt, never store plain text  
✅ **Use GenerateSecureToken** for all security-sensitive tokens  
✅ **Revoke refresh tokens** on password change  
✅ **Monitor token expiration** and warn users proactively  
✅ **Rehash old passwords** on login using `NeedsRehash`  
✅ **Rate limit** password validation endpoints to prevent brute force  

---

## Summary

### JWT Token Service (9 new methods)

- ✅ Token validation and expiration checks
- ✅ Claim extraction (user ID, username, roles)
- ✅ Token lifetime management
- ✅ ClaimsPrincipal parsing

### Password Hasher (5 new methods)

- ✅ Comprehensive password strength validation
- ✅ Secure password generation
- ✅ Token generation for resets/verification
- ✅ Hash rehashing detection

All methods are production-ready, tested, and follow security best practices!
