using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Data;

/// <summary>
/// Lớp ngữ cảnh cơ sở dữ liệu (Database Context) kết nối ứng dụng với Microsoft SQL Server
/// Sử dụng Entity Framework Core làm tầng trung gian ORM (Object-Relational Mapping)
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Bảng Brands (Thương hiệu / Hãng)
    /// </summary>
    public DbSet<Brand> Brands => Set<Brand>();

    /// <summary>
    /// Bảng Categories (Danh mục phân loại)
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Bảng Products (Thông tin chính của laptop)
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Bảng ProductSpecifications (Cấu hình phần cứng chi tiết)
    /// </summary>
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();

    /// <summary>
    /// Bảng ProductImages (Ảnh bổ sung của laptop)
    /// </summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    /// <summary>
    /// Bảng Users (Tài khoản người dùng và quản trị viên)
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Bảng Orders (Đơn đặt hàng)
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>
    /// Bảng OrderItems (Chi tiết các sản phẩm trong đơn)
    /// </summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <summary>
    /// Bảng Payments (Lịch sử thanh toán đơn hàng)
    /// </summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>
    /// Bảng OrderStatusHistories (Lịch sử thay đổi trạng thái đơn)
    /// </summary>
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    /// <summary>
    /// Bảng InstallmentContracts (Hợp đồng trả góp)
    /// </summary>
    public DbSet<InstallmentContract> InstallmentContracts => Set<InstallmentContract>();

    /// <summary>
    /// Bảng InstallmentSchedules (Lịch thanh toán các kỳ trả góp)
    /// </summary>
    public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();

    /// <summary>
    /// Bảng Colors (Danh mục Màu sắc)
    /// </summary>
    public DbSet<Color> Colors => Set<Color>();

    /// <summary>
    /// Bảng ProductVariants (Các phiên bản cụ thể của laptop: Màu + RAM + SSD)
    /// </summary>
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    /// <summary>
    /// Cấu hình nâng cao cho các bảng và mối quan hệ (Fluent API)
    /// </summary>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Cấu hình ràng buộc duy nhất cho Username và Email trong bảng Users
        mb.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Định dạng độ chính xác cho kiểu dữ liệu tiền tệ (decimal 18, 2)
        mb.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.DiscountPrice).HasPrecision(18, 2);
        });

        // Định dạng độ chính xác cho kích thước màn hình và trọng lượng
        mb.Entity<ProductSpecification>(entity =>
        {
            entity.Property(s => s.ScreenSizeInch).HasPrecision(18, 2);
            entity.Property(s => s.WeightKg).HasPrecision(18, 2);
        });

        // Cấu hình mối quan hệ 1 - 1 giữa Product và ProductSpecification
        // Khi xóa một Product thì cấu hình Specification tương ứng cũng tự động bị xóa (Cascade Delete)
        mb.Entity<Product>()
            .HasOne(p => p.Specification)
            .WithOne(s => s.Product)
            .HasForeignKey<ProductSpecification>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Cấu hình bảng Orders
        mb.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.Property(o => o.SubTotal).HasPrecision(18, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(o => o.OrderItems)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.Payments)
                .WithOne(p => p.Order)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(o => o.StatusHistories)
                .WithOne(h => h.Order)
                .HasForeignKey(h => h.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình bảng OrderItems
        mb.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
            entity.Property(i => i.TotalPrice).HasPrecision(18, 2);

            // Khi xóa Product, không tự động xóa các OrderItem đã mua trong lịch sử (Restrict Delete)
            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Cấu hình bảng Payments
        mb.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);
        });

        // Cấu hình bảng InstallmentContracts (Hợp đồng trả góp)
        mb.Entity<InstallmentContract>(entity =>
        {
            entity.HasIndex(c => c.ContractCode).IsUnique();
            entity.HasIndex(c => c.OrderId).IsUnique();

            entity.Property(c => c.PrepayPercent).HasPrecision(5, 2);
            entity.Property(c => c.PrepayAmount).HasPrecision(18, 2);
            entity.Property(c => c.TotalFinancedAmount).HasPrecision(18, 2);
            entity.Property(c => c.InterestRate).HasPrecision(5, 2);
            entity.Property(c => c.MonthlyInstallmentAmount).HasPrecision(18, 2);

            entity.HasOne(c => c.Order)
                .WithOne(o => o.InstallmentContract)
                .HasForeignKey<InstallmentContract>(c => c.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Schedules)
                .WithOne(s => s.Contract)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình bảng InstallmentSchedules (Lịch các kỳ)
        mb.Entity<InstallmentSchedule>(entity =>
        {
            entity.HasIndex(s => new { s.ContractId, s.PeriodNumber }).IsUnique();

            entity.Property(s => s.Amount).HasPrecision(18, 2);
            entity.Property(s => s.PaidAmount).HasPrecision(18, 2);

            entity.HasOne(s => s.Payment)
                .WithMany()
                .HasForeignKey(s => s.PaymentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình bảng Colors
        mb.Entity<Color>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // Cấu hình bảng ProductVariants
        mb.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(v => v.SKU).IsUnique();
            entity.HasIndex(v => new { v.ProductId, v.ColorId, v.RamGB, v.StorageGB }).IsUnique();

            entity.Property(v => v.Price).HasPrecision(18, 2);
            entity.Property(v => v.DiscountPrice).HasPrecision(18, 2);

            entity.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Color)
                .WithMany(c => c.Variants)
                .HasForeignKey(v => v.ColorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Cấu hình liên kết OrderItem -> ProductVariant
        mb.Entity<OrderItem>(entity =>
        {
            entity.HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}