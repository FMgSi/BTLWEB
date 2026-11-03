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
    /// Bảng SoldDevices (Từng chiếc laptop thực tế đã bàn giao theo Serial duy nhất)
    /// </summary>
    public DbSet<SoldDevice> SoldDevices => Set<SoldDevice>();

    /// <summary>
    /// Bảng ServiceCatalog (Danh mục dịch vụ bảo hành & sửa chữa)
    /// </summary>
    public DbSet<ServiceCatalogItem> ServiceCatalog => Set<ServiceCatalogItem>();

    /// <summary>
    /// Bảng ServiceTickets (Phiếu tiếp nhận yêu cầu bảo hành / sửa chữa)
    /// </summary>
    public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();

    /// <summary>
    /// Bảng ServiceQuotes (Báo giá dịch vụ cho khách hàng duyệt)
    /// </summary>
    public DbSet<ServiceQuote> ServiceQuotes => Set<ServiceQuote>();

    /// <summary>
    /// Bảng ServiceTicketHistories (Nhật ký xử lý phiếu bảo hành / sửa chữa)
    /// </summary>
    public DbSet<ServiceTicketHistory> ServiceTicketHistories => Set<ServiceTicketHistory>();

    /// <summary>
    /// Bảng Discounts (Mã giảm giá & chương trình khuyến mãi)
    /// </summary>
    public DbSet<Discount> Discounts => Set<Discount>();

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

        // Cấu hình bảng ProductImages: Mỗi sản phẩm + màu sắc + vị trí ô (1-4) chỉ có tối đa 1 ảnh
        mb.Entity<ProductImage>(entity =>
        {
            entity.HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Color)
                .WithMany()
                .HasForeignKey(i => i.ColorId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(i => new { i.ProductId, i.ColorId, i.DisplayOrder })
                .IsUnique()
                .HasFilter("[ColorId] IS NOT NULL");
        });

        // Cấu hình bảng Orders
        mb.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.HasIndex(o => o.IdempotencyKey)
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");

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

            entity.HasOne(p => p.Schedule)
                .WithMany()
                .HasForeignKey(p => p.ScheduleId)
                .OnDelete(DeleteBehavior.NoAction);
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
                .OnDelete(DeleteBehavior.NoAction);
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

        // Cấu hình bảng SoldDevices (Máy đã bán theo Serial)
        mb.Entity<SoldDevice>(entity =>
        {
            entity.HasIndex(d => d.SerialNumber).IsUnique();

            entity.HasOne(d => d.OrderItem)
                .WithMany()
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình bảng ServiceCatalog (Danh mục dịch vụ)
        mb.Entity<ServiceCatalogItem>(entity =>
        {
            entity.ToTable("ServiceCatalog");
            entity.HasIndex(s => s.ServiceCode).IsUnique();
            entity.Property(s => s.EstimatedCost).HasPrecision(18, 2);
        });

        // Cấu hình bảng ServiceTickets (Phiếu bảo hành / sửa chữa)
        mb.Entity<ServiceTicket>(entity =>
        {
            entity.HasIndex(t => t.TicketCode).IsUnique();
            entity.Property(t => t.FinalCost).HasPrecision(18, 2);

            entity.HasOne(t => t.Service)
                .WithMany()
                .HasForeignKey(t => t.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.SoldDevice)
                .WithMany(d => d.ServiceTickets)
                .HasForeignKey(t => t.SoldDeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Cấu hình bảng ServiceQuotes (Báo giá sửa chữa)
        mb.Entity<ServiceQuote>(entity =>
        {
            entity.Property(q => q.Amount).HasPrecision(18, 2);

            entity.HasOne(q => q.Ticket)
                .WithMany(t => t.Quotes)
                .HasForeignKey(q => q.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình bảng ServiceTicketHistories (Nhật ký xử lý phiếu)
        mb.Entity<ServiceTicketHistory>(entity =>
        {
            entity.HasOne(h => h.Ticket)
                .WithMany(t => t.Histories)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Cấu hình bảng Discounts (Mã giảm giá)
        mb.Entity<Discount>(entity =>
        {
            entity.HasIndex(d => d.Code).IsUnique();
            entity.Property(d => d.DiscountValue).HasPrecision(18, 2);
            entity.Property(d => d.MaxDiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.MinOrderAmount).HasPrecision(18, 2);
        });
    }
}