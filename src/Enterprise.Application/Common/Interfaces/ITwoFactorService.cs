namespace Enterprise.Application.Common.Interfaces;

public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new secret key for TOTP authentication
    /// </summary>
    string GenerateSecret();

    /// <summary>
    /// Generates a QR code URL for authenticator apps
    /// </summary>
    string GenerateQrCodeUrl(string email, string secret, string issuer = "Enterprise");

    /// <summary>
    /// Validates a TOTP code against a secret
    /// </summary>
    bool ValidateCode(string secret, string code);

    /// <summary>
    /// Generates recovery codes for backup authentication
    /// </summary>
    List<string> GenerateRecoveryCodes(int count = 8);

    /// <summary>
    /// Hashes a recovery code for secure storage
    /// </summary>
    string HashRecoveryCode(string code);

    /// <summary>
    /// Verifies a recovery code against stored hash
    /// </summary>
    bool VerifyRecoveryCode(string code, string hash);
}
