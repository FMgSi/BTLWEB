using System.Security.Cryptography;
using System.Text;

namespace LaptopStore.Helpers;

/// <summary>
/// Tiện ích mã hóa mật khẩu an toàn (Password Hashing)
/// Sử dụng thuật toán chuẩn công nghiệp PBKDF2 (Rfc2898DeriveBytes với SHA256 và Salt ngẫu nhiên)
/// Đồng thời hỗ trợ tương thích ngược (backward compatibility) với các tài khoản đã tạo bằng SHA256 cũ
/// Đáp ứng yêu cầu 3.5: An toàn bảo mật (Security) trong ứng dụng web
/// </summary>
public static class PasswordHelper
{
    private const string LegacySalt = "PCStore_Secret_Salt_@2024!";
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16; // 128-bit salt
    private const int HashSize = 32; // 256-bit subkey

    /// <summary>
    /// Băm mật khẩu bằng PBKDF2 với Salt ngẫu nhiên và 100,000 vòng lặp
    /// Định dạng chuỗi lưu trữ: PBKDF2$<iterations>$<salt_base64>$<hash_base64>
    /// </summary>
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

    /// <summary>
    /// Băm theo cơ chế cũ (Legacy SHA256 + Static Salt) dùng cho tương thích ngược
    /// </summary>
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

    /// <summary>
    /// Kiểm tra mật khẩu người dùng nhập vào có khớp với mã băm đã lưu trong CSDL hay không
    /// Tự động nhận diện định dạng PBKDF2 mới hoặc SHA256 cũ
    /// </summary>
    public static bool VerifyPassword(string inputPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        // 1. Kiểm tra nếu là mã băm PBKDF2 hiện đại
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

        // 2. Fallback kiểm tra mã băm SHA256 cũ
        var legacyHash = HashLegacyPassword(inputPassword);
        return string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
