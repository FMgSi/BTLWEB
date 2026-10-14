using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

/// <summary>
/// Đại diện cho tài khoản người dùng trong hệ thống (Khách hàng & Quản trị)
/// Phù hợp yêu cầu 3.1: Hệ thống có tối thiểu 2 lớp người dùng (Customer & Admin)
/// và yêu cầu 3.3: Có kiểm soát dữ liệu (Validation) và chú thích (Annotation)
/// </summary>
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
    [Display(Name = "Địa chỉ Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Mật khẩu đã được mã hóa băm một chiều (SHA256 + Salt) đảm bảo an toàn bảo mật
    /// </summary>
    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Phân quyền người dùng: "Customer" (Khách hàng) hoặc "Admin" (Quản trị viên)
    /// </summary>
    [Required]
    [StringLength(20)]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Customer";

    /// <summary>
    /// Mã ID tài khoản Google (nếu đăng nhập bằng Google OAuth)
    /// </summary>
    [StringLength(100)]
    public string? GoogleId { get; set; }

    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
