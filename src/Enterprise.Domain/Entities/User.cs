using Enterprise.Domain.Common;

namespace Enterprise.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    // Two-Factor Authentication
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? RecoveryCodes { get; set; } // JSON array of recovery codes

    // Navigation property for roles
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    public bool HasValidPasswordResetToken(string token) =>
        !string.IsNullOrEmpty(PasswordResetToken) &&
        PasswordResetToken == token &&
        PasswordResetTokenExpiry.HasValue &&
        PasswordResetTokenExpiry.Value > DateTime.UtcNow;
}
