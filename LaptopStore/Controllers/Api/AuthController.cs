using LaptopStore.Data;
using LaptopStore.Helpers;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Controllers.Api;

/// <summary>
/// Web API Xác thực Người dùng (Authentication Controller - RESTful API)
/// Hỗ trợ Đăng nhập bằng Mật khẩu (có Hashing SHA256 + Salt), Đăng nhập bằng Google, Đăng ký, Đăng xuất và Session
/// Phù hợp yêu cầu 3.1: Phân quyền Customer & Admin
/// và yêu cầu 3.3: Làm việc với Session, Cookies, RESTful API, Validation
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// POST /api/auth/login
    /// Đăng nhập bằng Email hoặc Tên đăng nhập và Mật khẩu (có kiểm tra mã Hash)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        var normalizedInput = request.EmailOrUsername.Trim().ToLower();

        // Tìm người dùng theo Email hoặc Username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedInput || u.Username.ToLower() == normalizedInput);

        if (user == null)
        {
            return BadRequest(new { success = false, message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        // Kiểm tra xem tài khoản có bị khóa không
        if (!user.IsActive)
        {
            return BadRequest(new { success = false, message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên." });
        }

        // Kiểm tra mật khẩu bằng hàm so khớp Hash
        bool isPasswordValid = PasswordHelper.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return BadRequest(new { success = false, message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        // Lưu thông tin người dùng vào Session (Yêu cầu 3.3: Làm việc với session)
        HttpContext.Session.SetString("UserId", user.UserId.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new
        {
            success = true,
            message = "Đăng nhập thành công!",
            user = new
            {
                id = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
                role = user.Role,
                avatarUrl = user.AvatarUrl
            }
        });
    }

    /// <summary>
    /// POST /api/auth/register
    /// Đăng ký tài khoản mới (Mật khẩu được băm SHA256 trước khi lưu vào CSDL)
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLower();

        // Kiểm tra trùng lặp Username hoặc Email
        bool usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername.ToLower());
        if (usernameExists)
        {
            return BadRequest(new { success = false, message = "Tên đăng nhập này đã được sử dụng." });
        }

        bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (emailExists)
        {
            return BadRequest(new { success = false, message = "Địa chỉ email này đã được đăng ký." });
        }

        // Mã hóa băm mật khẩu một chiều bằng SHA256 + Salt
        string passwordHash = PasswordHelper.HashPassword(request.Password);

        var newUser = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? normalizedUsername : request.FullName.Trim(),
            PasswordHash = passwordHash,
            Role = "Customer", // Mặc định tài khoản đăng ký là Khách hàng
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        // Tự động lưu Session đăng nhập ngay sau khi tạo tài khoản
        HttpContext.Session.SetString("UserId", newUser.UserId.ToString());
        HttpContext.Session.SetString("Username", newUser.Username);
        HttpContext.Session.SetString("Role", newUser.Role);

        return Ok(new
        {
            success = true,
            message = "Đăng ký tài khoản thành công!",
            user = new
            {
                id = newUser.UserId,
                username = newUser.Username,
                email = newUser.Email,
                fullName = newUser.FullName,
                role = newUser.Role,
                avatarUrl = newUser.AvatarUrl
            }
        });
    }

    /// <summary>
    /// POST /api/auth/google
    /// Tạm khóa cơ chế nhận email không xác minh từ client nhằm ngăn chặn chiếm quyền tài khoản
    /// </summary>
    [HttpPost("google")]
    public IActionResult GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        return BadRequest(new
        {
            success = false,
            message = "Chức năng đăng nhập Google hiện đang bảo trì để nâng cấp xác thực Google OAuth 2.0 an toàn. Vui lòng đăng nhập bằng Email/Tài khoản và Mật khẩu."
        });
    }

    /// <summary>
    /// GET /api/auth/me
    /// Lấy thông tin tài khoản đang đăng nhập trong Session hiện tại
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { success = false, message = "Tài khoản không tồn tại." });
        }

        if (!user.IsActive)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new { success = false, message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên." });
        }

        return Ok(new
        {
            success = true,
            user = new
            {
                id = user.UserId,
                username = user.Username,
                email = user.Email,
                fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName,
                role = user.Role,
                avatarUrl = user.AvatarUrl
            }
        });
    }

    /// <summary>
    /// POST /api/auth/logout
    /// Xóa phiên đăng nhập trong Session
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { success = true, message = "Đã đăng xuất thành công!" });
    }
}

// =========================================================================
// CÁC LỚP REQUEST DTO (DATA TRANSFER OBJECTS VỚI DATA ANNOTATIONS VALIDATION)
// Phù hợp yêu cầu 3.3: Có kiểm soát dữ liệu (Validation) và chú thích (Annotation)
// =========================================================================

public class LoginRequest
{
    [Required(ErrorMessage = "Vui lòng nhập Email hoặc Tên đăng nhập")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
    [RegularExpression(@"^[a-zA-Z0-9_]{3,20}$", ErrorMessage = "Tên đăng nhập từ 3 - 20 ký tự, chỉ gồm chữ cái, số và dấu gạch dưới")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Địa chỉ Email")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có tối thiểu 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }
}

public class GoogleAuthRequest
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email Google không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string? GoogleId { get; set; }
    public string? Avatar { get; set; }
}
