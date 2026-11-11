using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

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

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Customer";

    [StringLength(100)]
    public string? GoogleId { get; set; }

    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Trạng thái kích hoạt")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
