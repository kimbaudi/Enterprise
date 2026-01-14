namespace Enterprise.Application.Common.Interfaces;

public interface IPasswordHasher
{
    // Password hashing
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);

    // Password validation
    bool ValidatePasswordStrength(string password, out List<string> errors);
    bool MeetsMinimumRequirements(string password);

    // Password generation
    string GenerateRandomPassword(int length = 12);
    string GenerateSecureToken(int byteLength = 32);

    // Hash utilities
    bool NeedsRehash(string passwordHash);
}
