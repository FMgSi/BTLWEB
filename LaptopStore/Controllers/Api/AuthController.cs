using LaptopStore.Data;
using LaptopStore.Helpers;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { success = false, message = string.Join("; ", errors) });
        }

        var normalizedInput = request.EmailOrUsername.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedInput || u.Username.ToLower() == normalizedInput);

        if (user == null)
        {
            return BadRequest(new { success = false, message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        if (!user.IsActive)
        {
            return BadRequest(new { success = false, message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên." });
        }

        bool isPasswordValid = PasswordHelper.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return BadRequest(new { success = false, message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        HttpContext.Session.SetString("UserId", user.UserId.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("Role", user.Role);

        var (totalSpent, tier, phone) = await ResolveUserProfileDataAsync(user);

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
                avatarUrl = user.AvatarUrl,
                phoneNumber = phone,
                tier = tier,
                memberTier = tier,
                totalSpent = totalSpent
            }
        });
    }

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

        string passwordHash = PasswordHelper.HashPassword(request.Password);

        var newUser = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? normalizedUsername : request.FullName.Trim(),
            PasswordHash = passwordHash,
            Role = "Customer", 
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

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

    [HttpPost("google")]
    public IActionResult GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        return BadRequest(new
        {
            success = false,
            message = "Chức năng đăng nhập Google hiện đang bảo trì để nâng cấp xác thực Google OAuth 2.0 an toàn. Vui lòng đăng nhập bằng Email/Tài khoản và Mật khẩu."
        });
    }

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

        var (totalSpent, tier, phone) = await ResolveUserProfileDataAsync(user);

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
                avatarUrl = user.AvatarUrl,
                phoneNumber = phone,
                tier = tier,
                memberTier = tier,
                totalSpent = totalSpent
            }
        });
    }

    private async Task<(decimal TotalSpent, string MemberTier, string Phone)> ResolveUserProfileDataAsync(User user)
    {
        string phone = user.PhoneNumber ?? "";

        if (string.IsNullOrEmpty(phone))
        {
            var latestOrderPhone = await _context.Orders
                .Where(o => (o.UserId == user.UserId || (!string.IsNullOrEmpty(user.Email) && o.Email == user.Email) || (!string.IsNullOrEmpty(user.FullName) && o.CustomerName == user.FullName)) && !string.IsNullOrEmpty(o.PhoneNumber))
                .OrderByDescending(o => o.OrderId)
                .Select(o => o.PhoneNumber)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(latestOrderPhone))
            {
                phone = latestOrderPhone;
                user.PhoneNumber = latestOrderPhone;
            }
        }

        if (!string.IsNullOrEmpty(phone) || !string.IsNullOrEmpty(user.Email))
        {
            var unlinkedOrders = await _context.Orders
                .Where(o => o.UserId == null && (
                    (!string.IsNullOrEmpty(phone) && o.PhoneNumber == phone) ||
                    (!string.IsNullOrEmpty(user.Email) && o.Email == user.Email)
                ))
                .ToListAsync();

            if (unlinkedOrders.Any())
            {
                foreach (var o in unlinkedOrders)
                {
                    o.UserId = user.UserId;
                }
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch { }

        var completedOrders = await _context.Orders
            .Where(o => (o.UserId == user.UserId || (!string.IsNullOrEmpty(phone) && o.PhoneNumber == phone))
                && (o.OrderStatus == "Hoàn thành" || o.OrderStatus == "Đã giao" || o.PaymentStatus == "Đã thanh toán" || o.PaymentStatus == "Đã tất toán"))
            .ToListAsync();

        decimal totalSpent = completedOrders.Sum(o => o.TotalAmount);
        string tier = totalSpent >= 100000000m ? "Kim Cương" : (totalSpent >= 50000000m ? "Vàng" : (totalSpent >= 20000000m ? "Bạc" : "Đồng"));

        return (totalSpent, tier, phone);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { success = true, message = "Đã đăng xuất thành công!" });
    }
}

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
