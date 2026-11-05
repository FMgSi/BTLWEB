using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaptopStore.Tests;

public class DiscountServiceTests
{
    private AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .EnableSensitiveDataLogging()
            .Options;

        var db = new AppDbContext(options);

        // Seed danh mục và thương hiệu
        var catOffice = new Category { CategoryId = 1, CategoryName = "Laptop Văn phòng & Học tập", Description = "Office" };
        var catGaming = new Category { CategoryId = 2, CategoryName = "Laptop Gaming", Description = "Gaming" };
        db.Categories.AddRange(catOffice, catGaming);

        var brandAsus = new Brand { BrandId = 1, BrandName = "ASUS" };
        var brandApple = new Brand { BrandId = 2, BrandName = "Apple" };
        var brandDell = new Brand { BrandId = 3, BrandName = "Dell" };
        db.Brands.AddRange(brandAsus, brandApple, brandDell);

        // Seed sản phẩm
        var prodOffice = new Product
        {
            ProductId = 1,
            ProductName = "Dell Inspiron 15 3520",
            CategoryId = 1,
            BrandId = 3,
            Price = 14000000,
            StockQuantity = 10,
            IsActive = true
        };

        var prodGaming = new Product
        {
            ProductId = 2,
            ProductName = "ASUS ROG Strix G16 Gaming",
            CategoryId = 2,
            BrandId = 1,
            Price = 28000000,
            StockQuantity = 5,
            IsActive = true
        };

        var prodMac = new Product
        {
            ProductId = 3,
            ProductName = "Apple MacBook Air M3",
            CategoryId = 1,
            BrandId = 2,
            Price = 27000000,
            StockQuantity = 5,
            IsActive = true
        };

        db.Products.AddRange(prodOffice, prodGaming, prodMac);

        // Seed danh sách voucher
        var discounts = new List<Discount>
        {
            new Discount
            {
                DiscountId = 1,
                Code = "LAPTOP2026",
                Title = "Ưu đãi Laptop Văn phòng",
                DiscountType = "Percentage",
                DiscountValue = 10,
                MaxDiscountAmount = 1500000,
                MinOrderAmount = 10000000,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 100,
                UsedCount = 10,
                ApplicableScope = "OfficeOnly",
                IsActive = true
            },
            new Discount
            {
                DiscountId = 2,
                Code = "GAMING500K",
                Title = "Đại tiệc Laptop Gaming",
                DiscountType = "FixedAmount",
                DiscountValue = 500000,
                MinOrderAmount = 18000000,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = 50,
                UsedCount = 5,
                ApplicableScope = "GamingOnly",
                IsActive = true
            },
            new Discount
            {
                DiscountId = 3,
                Code = "TRIAN99",
                Title = "Tri ân khách hàng cũ",
                DiscountType = "FixedAmount",
                DiscountValue = 200000,
                MinOrderAmount = 10000000,
                StartDate = DateTime.UtcNow.AddDays(-60),
                EndDate = DateTime.UtcNow.AddDays(-3), // Đã hết hạn!
                UsageLimit = 200,
                UsedCount = 89,
                ApplicableScope = "All",
                IsActive = true
            },
            new Discount
            {
                DiscountId = 4,
                Code = "GAMINGX",
                Title = "Flash Sale Hết Lượt",
                DiscountType = "FixedAmount",
                DiscountValue = 1000000,
                MinOrderAmount = 18000000,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(10),
                UsageLimit = 10,
                UsedCount = 10, // Đã hết lượt dùng!
                ApplicableScope = "GamingOnly",
                IsActive = true
            },
            new Discount
            {
                DiscountId = 5,
                Code = "FIRSTORDER",
                Title = "Ưu đãi đơn đầu tiên",
                DiscountType = "Percentage",
                DiscountValue = 5,
                MaxDiscountAmount = 500000,
                MinOrderAmount = 5000000,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(30),
                UsageLimit = null,
                UsedCount = 0,
                ApplicableScope = "FirstOrderOnly",
                IsActive = true
            }
        };

        db.Discounts.AddRange(discounts);
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task GetAllDiscounts_DynamicStatus_ExpiredAndExhaustedCalculatedCorrectly()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_Status_" + Guid.NewGuid());
        var service = new DiscountService(db);

        var list = await service.GetAllDiscountsAsync();

        var triAn = list.FirstOrDefault(d => d.Code == "TRIAN99");
        Assert.NotNull(triAn);
        Assert.Equal("Hết hạn", triAn.Status);
        Assert.Equal("badge-out-of-stock", triAn.StatusBadgeClass);

        var gamingX = list.FirstOrDefault(d => d.Code == "GAMINGX");
        Assert.NotNull(gamingX);
        Assert.Equal("Hết lượt dùng", gamingX.Status);

        var laptop = list.FirstOrDefault(d => d.Code == "LAPTOP2026");
        Assert.NotNull(laptop);
        Assert.Equal("Đang hoạt động", laptop.Status);
        Assert.Equal("badge-in-stock", laptop.StatusBadgeClass);
    }

    [Fact]
    public async Task ValidateDiscount_ValidVoucher_CalculatesAccurateCappedAmount()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_Valid_" + Guid.NewGuid());
        var service = new DiscountService(db);

        var items = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest { ProductId = 1, Quantity = 1 } // Dell Inspiron Office
        };

        // SubTotal 14,000,000: 10% là 1,400,000 (dưới trần 1,500,000)
        var res1 = await service.ValidateDiscountForCartAsync("LAPTOP2026", 14000000, items);
        Assert.True(res1.IsValid);
        Assert.Equal(1400000, res1.DiscountAmount);
        Assert.Equal(12600000, res1.TotalPayable);

        // SubTotal 20,000,000: 10% là 2,000,000 (vượt trần 1,500,000 -> lấy trần 1,500,000)
        var res2 = await service.ValidateDiscountForCartAsync("LAPTOP2026", 20000000, items);
        Assert.True(res2.IsValid);
        Assert.Equal(1500000, res2.DiscountAmount);
        Assert.Equal(18500000, res2.TotalPayable);
    }

    [Fact]
    public async Task ValidateDiscount_BelowMinOrderAmount_RejectsWithDetailedDeficitMessage()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_MinAmount_" + Guid.NewGuid());
        var service = new DiscountService(db);

        var items = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest { ProductId = 1, Quantity = 1 }
        };

        // Đơn 8tr nhưng voucher yêu cầu tối thiểu 10tr
        var res = await service.ValidateDiscountForCartAsync("LAPTOP2026", 8000000, items);
        Assert.False(res.IsValid);
        Assert.Contains("Cần mua thêm 2.000.000", res.Message);
        Assert.Equal(2000000, res.NeedMoreAmount);
    }

    [Fact]
    public async Task ValidateDiscount_ScopeRestriction_OfficeOnlyRejectsGamingLaptop()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_Scope_" + Guid.NewGuid());
        var service = new DiscountService(db);

        var gamingItems = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest { ProductId = 2, Quantity = 1 } // ROG Gaming
        };

        var res = await service.ValidateDiscountForCartAsync("LAPTOP2026", 28000000, gamingItems);
        Assert.False(res.IsValid);
        Assert.Contains("Laptop Văn phòng & Học tập", res.Message);
    }

    [Fact]
    public async Task ValidateDiscount_ExpiredVoucher_RejectsWithExpirationDate()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_Expired_" + Guid.NewGuid());
        var service = new DiscountService(db);

        var items = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest { ProductId = 1, Quantity = 1 }
        };

        var res = await service.ValidateDiscountForCartAsync("TRIAN99", 15000000, items);
        Assert.False(res.IsValid);
        Assert.Contains("hết hạn sử dụng", res.Message);
    }

    [Fact]
    public async Task ValidateDiscount_FirstOrderOnly_RejectsReturningCustomer()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_FirstOrder_" + Guid.NewGuid());
        var service = new DiscountService(db);

        // Tạo 1 đơn hàng cũ đã tồn tại cho số điện thoại 0988776655
        db.Orders.Add(new Order
        {
            OrderCode = "PC-OLD-01",
            CustomerName = "Khách Cũ",
            PhoneNumber = "0988776655",
            ShippingAddress = "Hà Nội",
            SubTotal = 10000000,
            TotalAmount = 10000000,
            OrderStatus = "Hoàn thành"
        });
        await db.SaveChangesAsync();

        var items = new List<CreateOrderItemRequest>
        {
            new CreateOrderItemRequest { ProductId = 1, Quantity = 1 }
        };

        var res = await service.ValidateDiscountForCartAsync("FIRSTORDER", 14000000, items, phone: "0988776655");
        Assert.False(res.IsValid);
        Assert.Contains("chỉ dành riêng cho đơn hàng đầu tiên", res.Message);
    }

    [Fact]
    public async Task CreateOrder_WithValidDiscount_DeductsAmountIncrementsUsedCountAndCalculatesInstallment()
    {
        using var db = CreateInMemoryDbContext("Test_Order_Discount_" + Guid.NewGuid());
        var installmentService = new InstallmentService(db);
        var discountService = new DiscountService(db);
        var orderService = new OrderService(db, installmentService, discountService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Trần Văn B",
            PhoneNumber = "0912345678",
            ShippingAddress = "123 Cầu Giấy, Hà Nội",
            PaymentMethod = "COD",
            PurchaseMode = "Installment",
            TermMonths = 6,
            DiscountCode = "LAPTOP2026",
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = 1, Quantity = 1 } // Dell 14,000,000
            }
        };

        var (success, msg, order) = await orderService.CreateOrderAsync(req);

        Assert.True(success, msg);
        Assert.NotNull(order);
        Assert.Equal(14000000, order.SubTotal);
        Assert.Equal(1400000, order.DiscountAmount); // Giảm 10% = 1.4tr
        Assert.Equal("LAPTOP2026", order.DiscountCode);
        Assert.Equal(12600000, order.TotalAmount); // 14tr - 1.4tr = 12.6tr

        // Kiểm tra trả góp: Tính chính xác 30% trên 12,600,000 (không phải 14,000,000)
        Assert.Equal(3780000, order.PrepayAmount); // 30% của 12.6tr = 3,780,000
        Assert.Equal(8820000, order.InstallmentContract?.TotalFinancedAmount); // Dư nợ 70% còn lại = 8,820,000
        Assert.Equal(12600000, order.RemainingDebt); // Toàn bộ công nợ đơn khi chưa trả khoản trả trước

        // Kiểm tra số lượt đã dùng của voucher tăng lên 1
        var voucherInDb = await db.Discounts.FirstOrDefaultAsync(d => d.Code == "LAPTOP2026");
        Assert.NotNull(voucherInDb);
        Assert.Equal(11, voucherInDb.UsedCount); // Ban đầu seed là 10, nay lên 11
    }

    [Fact]
    public async Task DeleteDiscount_WhenUsedInOrders_SwitchesToInactiveInsteadOfHardDelete()
    {
        using var db = CreateInMemoryDbContext("Test_Discount_Delete_" + Guid.NewGuid());
        var service = new DiscountService(db);

        // LAPTOP2026 đã có 10 lượt sử dụng
        var (success, msg) = await service.DeleteDiscountAsync(1, "Admin");
        Assert.True(success);
        Assert.Contains("Ngừng áp dụng", msg);

        var voucherInDb = await db.Discounts.FindAsync(1);
        Assert.NotNull(voucherInDb); // Vẫn còn trong CSDL để bảo vệ lịch sử hóa đơn
        Assert.False(voucherInDb.IsActive); // Đã chuyển sang Ngừng áp dụng
    }

    [Fact]
    public async Task CreateOrder_WithMemberTierSilver_Applies5PercentDiscount()
    {
        using var db = CreateInMemoryDbContext("Test_MemberTier_Silver_" + Guid.NewGuid());
        var installmentService = new InstallmentService(db);
        var discountService = new DiscountService(db);
        var orderService = new OrderService(db, installmentService, discountService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Khách Hàng Bạc",
            PhoneNumber = "0987654321",
            ShippingAddress = "Hà Nội",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            MemberTier = "Silver",
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = 1, Quantity = 1 } // 14,000,000
            }
        };

        var (success, msg, order) = await orderService.CreateOrderAsync(req);

        Assert.True(success, msg);
        Assert.NotNull(order);
        Assert.Equal(14000000, order.SubTotal);
        Assert.Equal(700000, order.DiscountAmount); // 5% của 14tr = 700k
        Assert.Equal(13300000, order.TotalAmount); // 14tr - 700k = 13.3tr
    }

    [Fact]
    public async Task CreateOrder_WithMemberTierGoldAndVoucher_AppliesBothDiscounts()
    {
        using var db = CreateInMemoryDbContext("Test_MemberTier_Gold_" + Guid.NewGuid());
        var installmentService = new InstallmentService(db);
        var discountService = new DiscountService(db);
        var orderService = new OrderService(db, installmentService, discountService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Khách Hàng Vàng",
            PhoneNumber = "0987654321",
            ShippingAddress = "TP.HCM",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            MemberTier = "Gold",
            DiscountCode = "LAPTOP2026", // Giảm 10%
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = 1, Quantity = 1 } // 14,000,000
            }
        };

        var (success, msg, order) = await orderService.CreateOrderAsync(req);

        Assert.True(success, msg);
        Assert.NotNull(order);
        Assert.Equal(14000000, order.SubTotal);
        // Hạng Vàng: 10% = 1.4tr, Voucher: 10% = 1.4tr => Tổng giảm = 2.8tr
        Assert.Equal(2800000, order.DiscountAmount);
        Assert.Equal(11200000, order.TotalAmount);
    }

    [Fact]
    public async Task CreateOrder_AutoAppliesMemberTier_BasedOnHistoricalSpendingWithoutRequestTier()
    {
        using var db = CreateInMemoryDbContext("Test_AutoMemberTier_" + Guid.NewGuid());
        var installmentService = new InstallmentService(db);
        var discountService = new DiscountService(db);
        var orderService = new OrderService(db, installmentService, discountService);

        // Tạo 1 tài khoản đã chi tiêu 60 triệu (đạt Hạng Vàng: >= 50tr)
        var user = new User
        {
            UserId = 99,
            Username = "golduser",
            Email = "golduser@gmail.com",
            PhoneNumber = "0988776655"
        };
        db.Users.Add(user);

        db.Orders.Add(new Order
        {
            OrderId = 991,
            OrderCode = "ORD-PREV-GOLD",
            UserId = 99,
            CustomerName = "Gold User",
            PhoneNumber = "0988776655",
            OrderStatus = "Hoàn thành",
            PaymentStatus = "Đã thanh toán",
            TotalAmount = 60000000,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        });
        await db.SaveChangesAsync();

        // Khách hàng đặt đơn hàng mới, KHÔNG truyền MemberTier (để null)
        var req = new CreateOrderRequest
        {
            CustomerName = "Gold User",
            PhoneNumber = "0988776655",
            ShippingAddress = "Đà Nẵng",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            MemberTier = null, // Không truyền hạng từ client, server phải tự động xác định
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = 1, Quantity = 1 } // 14,000,000
            }
        };

        var (success, msg, order) = await orderService.CreateOrderAsync(req, userId: 99);

        Assert.True(success, msg);
        Assert.NotNull(order);
        Assert.Equal(14000000, order.SubTotal);
        // Tự động được giảm 10% Hạng Vàng = 1.4tr
        Assert.Equal(1400000, order.DiscountAmount);
        Assert.Equal(12600000, order.TotalAmount);
    }
}
