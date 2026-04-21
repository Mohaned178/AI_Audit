using System.Security.Cryptography;
using System.Text;

namespace AIUsageGuard.Application.Security;

public static class PasswordHashing
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join(
            '.',
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool VerifyPassword(string password, string encodedHash)
    {
        var parts = encodedHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
