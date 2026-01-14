using Enterprise.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Enterprise.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public bool ValidatePasswordStrength(string password, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return false;
        }

        if (password.Length < MinPasswordLength)
            errors.Add($"Password must be at least {MinPasswordLength} characters long");

        if (password.Length > MaxPasswordLength)
            errors.Add($"Password must not exceed {MaxPasswordLength} characters");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            errors.Add("Password must contain at least one uppercase letter");

        if (!Regex.IsMatch(password, @"[a-z]"))
            errors.Add("Password must contain at least one lowercase letter");

        if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add("Password must contain at least one digit");

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            errors.Add("Password must contain at least one special character");

        // Check for common weak passwords
        var weakPasswords = new[] { "password", "12345678", "qwerty", "admin", "letmein" };
        if (weakPasswords.Any(weak => password.ToLower().Contains(weak)))
            errors.Add("Password contains common weak patterns");

        return errors.Count == 0;
    }

    public bool MeetsMinimumRequirements(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            return false;

        return Regex.IsMatch(password, @"[A-Z]") &&
               Regex.IsMatch(password, @"[a-z]") &&
               Regex.IsMatch(password, @"[0-9]");
    }

    public string GenerateRandomPassword(int length = 12)
    {
        if (length < MinPasswordLength)
            length = MinPasswordLength;

        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}";
        const string allChars = lowercase + uppercase + digits + special;

        using var rng = RandomNumberGenerator.Create();
        var password = new char[length];

        // Ensure at least one of each required character type
        password[0] = lowercase[GetRandomIndex(rng, lowercase.Length)];
        password[1] = uppercase[GetRandomIndex(rng, uppercase.Length)];
        password[2] = digits[GetRandomIndex(rng, digits.Length)];
        password[3] = special[GetRandomIndex(rng, special.Length)];

        // Fill the rest randomly
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[GetRandomIndex(rng, allChars.Length)];
        }

        // Shuffle the array
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = GetRandomIndex(rng, i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    public string GenerateSecureToken(int byteLength = 32)
    {
        var tokenBytes = new byte[byteLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public bool NeedsRehash(string passwordHash)
    {
        // BCrypt hashes start with $2a$, $2b$, or $2y$ followed by cost factor
        // Check if the cost factor is less than our current (12)
        try
        {
            var parts = passwordHash.Split('$');
            if (parts.Length >= 3 && int.TryParse(parts[2], out var costFactor))
            {
                return costFactor < 12;
            }
        }
        catch
        {
            // If we can't parse it, assume it needs rehashing
            return true;
        }

        return false;
    }

    private static int GetRandomIndex(RandomNumberGenerator rng, int max)
    {
        var buffer = new byte[4];
        rng.GetBytes(buffer);
        var randomInt = BitConverter.ToUInt32(buffer, 0);
        return (int)(randomInt % max);
    }
}
