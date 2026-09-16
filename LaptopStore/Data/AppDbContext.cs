using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Data;

/// <summary>
/// Lớp ngữ cảnh cơ sở dữ liệu (Database Context) kết nối ứng dụng với MySQL
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
    /// Cấu hình nâng cao cho các bảng và mối quan hệ (Fluent API)
    /// </summary>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

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
    }
}