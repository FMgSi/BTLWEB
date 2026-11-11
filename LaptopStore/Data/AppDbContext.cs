using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    public DbSet<InstallmentContract> InstallmentContracts => Set<InstallmentContract>();

    public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();

    public DbSet<Color> Colors => Set<Color>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<SoldDevice> SoldDevices => Set<SoldDevice>();

    public DbSet<ServiceCatalogItem> ServiceCatalog => Set<ServiceCatalogItem>();

    public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();

    public DbSet<ServiceQuote> ServiceQuotes => Set<ServiceQuote>();

    public DbSet<ServiceTicketHistory> ServiceTicketHistories => Set<ServiceTicketHistory>();

    public DbSet<Discount> Discounts => Set<Discount>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        mb.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.DiscountPrice).HasPrecision(18, 2);
        });

        mb.Entity<ProductSpecification>(entity =>
        {
            entity.Property(s => s.ScreenSizeInch).HasPrecision(18, 2);
            entity.Property(s => s.WeightKg).HasPrecision(18, 2);
        });

        mb.Entity<Product>()
            .HasOne(p => p.Specification)
            .WithOne(s => s.Product)
            .HasForeignKey<ProductSpecification>(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

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

        mb.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
            entity.Property(i => i.TotalPrice).HasPrecision(18, 2);

            entity.HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasPrecision(18, 2);

            entity.HasOne(p => p.Schedule)
                .WithMany()
                .HasForeignKey(p => p.ScheduleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

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

        mb.Entity<Color>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

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

        mb.Entity<OrderItem>(entity =>
        {
            entity.HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<SoldDevice>(entity =>
        {
            entity.HasIndex(d => d.SerialNumber).IsUnique();

            entity.HasOne(d => d.OrderItem)
                .WithMany()
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ServiceCatalogItem>(entity =>
        {
            entity.ToTable("ServiceCatalog");
            entity.HasIndex(s => s.ServiceCode).IsUnique();
            entity.Property(s => s.EstimatedCost).HasPrecision(18, 2);
        });

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

        mb.Entity<ServiceQuote>(entity =>
        {
            entity.Property(q => q.Amount).HasPrecision(18, 2);

            entity.HasOne(q => q.Ticket)
                .WithMany(t => t.Quotes)
                .HasForeignKey(q => q.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ServiceTicketHistory>(entity =>
        {
            entity.HasOne(h => h.Ticket)
                .WithMany(t => t.Histories)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Discount>(entity =>
        {
            entity.HasIndex(d => d.Code).IsUnique();
            entity.Property(d => d.DiscountValue).HasPrecision(18, 2);
            entity.Property(d => d.MaxDiscountAmount).HasPrecision(18, 2);
            entity.Property(d => d.MinOrderAmount).HasPrecision(18, 2);
        });
    }
}
