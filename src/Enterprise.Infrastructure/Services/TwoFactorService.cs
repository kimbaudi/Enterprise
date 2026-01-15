using Enterprise.Application.Common.Interfaces;
using OtpNet;
using System.Security.Cryptography;
using System.Text;

namespace Enterprise.Infrastructure.Services;

public class TwoFactorService : ITwoFactorService
{
    private const int SecretLength = 20; // 160 bits
    private const int RecoveryCodeLength = 8;

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(SecretLength);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUrl(string email, string secret, string issuer = "Enterprise")
    {
        // Format: otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);

            // Allow 1 step before and after to account for time sync issues
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public List<string> GenerateRecoveryCodes(int count = 8)
    {
        var codes = new List<string>();

        for (int i = 0; i < count; i++)
        {
            codes.Add(GenerateRecoveryCode());
        }

        return codes;
    }

    private static string GenerateRecoveryCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = RandomNumberGenerator.Create();
        var code = new char[RecoveryCodeLength];

        for (int i = 0; i < RecoveryCodeLength; i++)
        {
            var randomBytes = new byte[4];
            random.GetBytes(randomBytes);
            var randomInt = BitConverter.ToUInt32(randomBytes, 0);
            code[i] = chars[(int)(randomInt % chars.Length)];
        }

        // Format as XXXX-XXXX for readability
        return $"{new string(code, 0, 4)}-{new string(code, 4, 4)}";
    }

    public string HashRecoveryCode(string code)
    {
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyRecoveryCode(string code, string hash)
    {
        var codeHash = HashRecoveryCode(code);
        return codeHash == hash;
    }
}
