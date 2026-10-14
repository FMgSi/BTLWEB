using System.Security.Cryptography;
using System.Text;

namespace LaptopStore.Helpers;

/// <summary>
/// Tiện ích mã hóa mật khẩu một chiều (Password Hashing)
/// Thiết kế đơn giản, rõ ràng, dễ hiểu cho người mới học ASP.NET Core
/// Sử dụng thuật toán SHA256 chuẩn kết hợp chuỗi khóa bảo mật (Salt)
/// Đáp ứng yêu cầu 3.5: An toàn bảo mật (Security) trong ứng dụng web
/// </summary>
public static class PasswordHelper
{
    // Chuỗi salt bí mật để bảo vệ mật khẩu chống lại kỹ thuật dò bảng Rainbow Table
    private const string Salt = "PCStore_Secret_Salt_@2024!";

    /// <summary>
    /// Băm chuỗi mật khẩu thô (Plain Text) sang chuỗi mã hóa bảo mật SHA256 (dạng chuỗi Hex 64 ký tự)
    /// </summary>
    public static string HashPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword)) return string.Empty;

        using var sha256 = SHA256.Create();
        // Ghép mật khẩu với chuỗi Salt bí mật
        var combined = plainPassword + Salt;
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hashBytes = sha256.ComputeHash(bytes);

        // Chuyển mảng byte sang định dạng chuỗi Hexadecimal dễ lưu trữ vào MySQL
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Kiểm tra mật khẩu người dùng nhập vào có khớp với mã băm đã lưu trong CSDL hay không
    /// </summary>
    public static bool VerifyPassword(string inputPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
            return false;

        var inputHash = HashPassword(inputPassword);
        return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
