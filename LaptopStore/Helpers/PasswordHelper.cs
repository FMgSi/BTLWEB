using System.Security.Cryptography;
using System.Text;

namespace LaptopStore.Helpers;

public static class PasswordHelper
{
    private const string LegacySalt = "PCStore_Secret_Salt_@2024!";
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16; 
    private const int HashSize = 32; 

    public static string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword)) return string.Empty;

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainPassword),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        );

        return $"PBKDF2${Pbkdf2Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static string HashLegacyPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword)) return string.Empty;

        using var sha256 = SHA256.Create();
        var combined = plainPassword + LegacySalt;
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hashBytes = sha256.ComputeHash(bytes);

        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    public static bool VerifyPassword(string inputPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        if (storedHash.StartsWith("PBKDF2$"))
        {
            try
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4) return false;

                int iterations = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(inputPassword),
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length
                );

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        var legacyHash = HashLegacyPassword(inputPassword);
        return string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
