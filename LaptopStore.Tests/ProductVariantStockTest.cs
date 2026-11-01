using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Controllers;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaptopStore.Tests;

/// <summary>
/// Bộ kiểm thử toàn diện nghiệp vụ Màu sắc (Color) và Phiên bản (ProductVariant)
/// Kiểm tra chặt chẽ:
/// 1. Tồn kho độc lập theo từng màu sắc (Bạc tồn 1, Đen tồn 2).
/// 2. Hai khách cùng mua chiếc máy màu Bạc cuối cùng -> Khách 2 báo hết hàng, không âm kho, tồn màu Đen giữ nguyên.
/// 3. Giỏ hàng nhiều món, 1 món hết hàng -> Rollback toàn bộ transaction (không tạo đơn, không sinh hợp đồng rác).
/// 4. Hủy đơn hàng -> Hoàn kho đúng chính xác phiên bản màu sắc đã đặt.
/// 5. Đơn trả góp 0% tích hợp phiên bản màu sắc -> Trừ kho đúng màu, tính toán 30% trả trước và lịch kỳ chuẩn xác.
/// 6. CRUD ColorsController -> Chống trùng tên màu, ngăn chặn xóa màu đang có phiên bản sử dụng.
/// </summary>
public class ProductVariantStockTest
{
    private static (AppDbContext Db, Product Product, Color ColorSilver, Color ColorBlack, ProductVariant VariantSilver, ProductVariant VariantBlack) CreateTestSetup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new AppDbContext(options);

        // 1. Seed Brand, Category
        var brand = new Brand { BrandId = 1, BrandName = "Dell" };
        var category = new Category { CategoryId = 1, CategoryName = "Laptop văn phòng" };
        context.Brands.Add(brand);
        context.Categories.Add(category);

        // 2. Seed Colors
        var silver = new Color { ColorId = 1, Name = "Bạc", HexCode = "#E5E7EB", IsActive = true };
        var black = new Color { ColorId = 2, Name = "Đen", HexCode = "#18181B", IsActive = true };
        context.Colors.AddRange(silver, black);

        // 3. Seed Product: Dell Inspiron 15 (Tổng tồn kho = 3 máy)
        var product = new Product
        {
            ProductId = 1,
            ProductName = "Dell Inspiron 15 3520",
            BrandId = 1,
            CategoryId = 1,
            Price = 17990000m,
            DiscountPrice = null,
            StockQuantity = 3,
            ThumbnailUrl = "/images/products/dell-inspiron-15.png",
            IsActive = true
        };
        context.Products.Add(product);

        // 4. Seed Variants theo đúng ví dụ nghiệp vụ:
        // - Dell Bạc 16GB/512GB: Tồn 1
        // - Dell Đen 16GB/512GB: Tồn 2
        var vSilver = new ProductVariant
        {
            VariantId = 101,
            ProductId = 1,
            ColorId = 1,
            RamGB = 16,
            StorageGB = 512,
            SKU = "DELL-1-SLV-16-512",
            StockQuantity = 1,
            Price = 17990000m,
            DiscountPrice = null,
            IsActive = true
        };

        var vBlack = new ProductVariant
        {
            VariantId = 102,
            ProductId = 1,
            ColorId = 2,
            RamGB = 16,
            StorageGB = 512,
            SKU = "DELL-1-BLK-16-512",
            StockQuantity = 2,
            Price = 17990000m,
            DiscountPrice = null,
            IsActive = true
        };

        context.ProductVariants.AddRange(vSilver, vBlack);
        context.SaveChanges();

        return (context, product, silver, black, vSilver, vBlack);
    }

    [Fact]
    public async Task TwoCustomers_BuyingLastSilverLaptop_OnlyOneSucceeds_BlackStockRemainsUnchanged()
    {
        // ARRANGE
        var (db, product, silver, black, vSilver, vBlack) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var reqCustomer1 = new CreateOrderRequest
        {
            CustomerName = "Nguyễn Văn A",
            PhoneNumber = "0911111111",
            ShippingAddress = "123 Cầu Giấy, Hà Nội",
            PaymentMethod = "VietQR",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 101, // Máy Bạc (tồn 1)
                    ColorName = "Bạc",
                    Quantity = 1
                }
            }
        };

        var reqCustomer2 = new CreateOrderRequest
        {
            CustomerName = "Trần Thị B",
            PhoneNumber = "0922222222",
            ShippingAddress = "456 Đống Đa, Hà Nội",
            PaymentMethod = "VietQR",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 101, // Máy Bạc (chiếc cuối cùng vừa bị mua)
                    ColorName = "Bạc",
                    Quantity = 1
                }
            }
        };

        // ACT
        // Khách 1 mua chiếc máy màu Bạc cuối cùng
        var res1 = await orderService.CreateOrderAsync(reqCustomer1);

        // Khách 2 cũng cố gắng mua chiếc máy màu Bạc
        var res2 = await orderService.CreateOrderAsync(reqCustomer2);

        // ASSERT
        // 1. Khách 1 thành công
        Assert.True(res1.Success);
        Assert.NotNull(res1.Order);
        Assert.Equal("Bạc", res1.Order.Items[0].ColorName);
        Assert.Equal(101, res1.Order.Items[0].VariantId);

        // 2. Khách 2 thất bại vì hết hàng
        Assert.False(res2.Success);
        Assert.Contains("không đủ tồn kho", res2.Message, StringComparison.OrdinalIgnoreCase);

        // 3. Tồn kho máy Bạc giảm từ 1 về 0 (không bị âm)
        var freshSilver = await db.ProductVariants.FindAsync(101);
        Assert.NotNull(freshSilver);
        Assert.Equal(0, freshSilver.StockQuantity);

        // 4. Tồn kho máy Đen giữ nguyên = 2 (tách biệt màu sắc tuyệt đối)
        var freshBlack = await db.ProductVariants.FindAsync(102);
        Assert.NotNull(freshBlack);
        Assert.Equal(2, freshBlack.StockQuantity);

        // 5. Tổng tồn kho Product giảm từ 3 xuống 2
        var freshProduct = await db.Products.FindAsync(1);
        Assert.NotNull(freshProduct);
        Assert.Equal(2, freshProduct.StockQuantity);
    }

    [Fact]
    public async Task CartWithTwoItems_WhenSecondItemOutOfStock_RollbacksEntireTransaction()
    {
        // ARRANGE
        var (db, product, silver, black, vSilver, vBlack) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        // Giỏ hàng gồm:
        // Món 1: 1 máy Đen (tồn 2)
        // Món 2: 2 máy Bạc (trong khi kho chỉ còn 1 máy Bạc)
        var cartRequest = new CreateOrderRequest
        {
            CustomerName = "Lê Hoàng Nam",
            PhoneNumber = "0933333333",
            ShippingAddress = "789 Ba Đình, Hà Nội",
            PaymentMethod = "VietQR",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 102, // Đen, mua 1 máy
                    ColorName = "Đen",
                    Quantity = 1
                },
                new()
                {
                    ProductId = 1,
                    VariantId = 101, // Bạc, đòi mua 2 máy (vượt tồn kho vì Bạc chỉ còn 1)
                    ColorName = "Bạc",
                    Quantity = 2
                }
            }
        };

        // ACT
        var res = await orderService.CreateOrderAsync(cartRequest);

        // ASSERT
        // Đặt hàng thất bại do món thứ 2 thiếu hàng
        Assert.False(res.Success);
        Assert.Contains("không đủ tồn kho", res.Message, StringComparison.OrdinalIgnoreCase);

        // Transaction phải ROLLBACK TOÀN BỘ:
        // Món 1 (máy Đen) KHÔNG ĐƯỢC PHÉP bị trừ (vẫn còn nguyên 2 máy)
        var freshBlack = await db.ProductVariants.FindAsync(102);
        Assert.NotNull(freshBlack);
        Assert.Equal(2, freshBlack.StockQuantity);

        // Món 2 (máy Bạc) vẫn còn nguyên 1 máy
        var freshSilver = await db.ProductVariants.FindAsync(101);
        Assert.NotNull(freshSilver);
        Assert.Equal(1, freshSilver.StockQuantity);

        // Không sinh đơn hàng rác trong CSDL
        var orderCount = await db.Orders.CountAsync();
        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task CancelOrder_RestoresExactColorStockToVariantAndProduct()
    {
        // ARRANGE
        var (db, product, silver, black, vSilver, vBlack) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        // Khách đặt mua 1 máy Bạc
        var orderReq = new CreateOrderRequest
        {
            CustomerName = "Vũ Hải Đăng",
            PhoneNumber = "0944444444",
            ShippingAddress = "12 Hai Bà Trưng, Hà Nội",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 101, // Bạc
                    ColorName = "Bạc",
                    Quantity = 1
                }
            }
        };

        var orderRes = await orderService.CreateOrderAsync(orderReq);
        Assert.True(orderRes.Success);
        int orderId = orderRes.Order!.OrderId;

        // Kiểm tra sau khi đặt: Bạc giảm từ 1 về 0
        var silverAfterBuy = await db.ProductVariants.FindAsync(101);
        Assert.Equal(0, silverAfterBuy!.StockQuantity);

        // ACT
        // Khách hoặc Admin hủy đơn hàng
        var cancelRes = await orderService.UpdateOrderStatusAsync(orderId, "Đã hủy", "Khách đổi ý muốn mua màu khác");

        // ASSERT
        Assert.True(cancelRes.Success);

        // Máy Bạc phải được HOÀN KHO ĐÚNG 1 CHIẾC (từ 0 lên lại 1)
        var silverAfterCancel = await db.ProductVariants.FindAsync(101);
        Assert.NotNull(silverAfterCancel);
        Assert.Equal(1, silverAfterCancel.StockQuantity);

        // Máy Đen không bị ảnh hưởng (vẫn là 2)
        var blackAfterCancel = await db.ProductVariants.FindAsync(102);
        Assert.NotNull(blackAfterCancel);
        Assert.Equal(2, blackAfterCancel.StockQuantity);

        // Tổng tồn kho Product được hoàn về lại 3
        var productAfterCancel = await db.Products.FindAsync(1);
        Assert.NotNull(productAfterCancel);
        Assert.Equal(3, productAfterCancel.StockQuantity);
    }

    [Fact]
    public async Task InstallmentOrder_WithColorVariant_CalculatesPrepayAndDeductsVariantStock()
    {
        // ARRANGE
        var (db, product, silver, black, vSilver, vBlack) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        // Đặt mua trả góp 1 chiếc máy màu Đen (17.990.000đ), kỳ hạn 6 tháng
        var orderReq = new CreateOrderRequest
        {
            CustomerName = "Phạm Quang Minh",
            PhoneNumber = "0955555555",
            ShippingAddress = "88 Hoàng Hoa Thám, Hà Nội",
            PaymentMethod = "VietQR",
            PurchaseMode = "Installment",
            TermMonths = 6,
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 102, // Đen
                    ColorName = "Đen",
                    Quantity = 1
                }
            }
        };

        // ACT
        var res = await orderService.CreateOrderAsync(orderReq);

        // ASSERT
        Assert.True(res.Success);
        Assert.NotNull(res.Order);

        // 1. Tồn kho máy Đen giảm từ 2 xuống 1
        var freshBlack = await db.ProductVariants.FindAsync(102);
        Assert.NotNull(freshBlack);
        Assert.Equal(1, freshBlack.StockQuantity);

        // 2. Chi tiết mặt hàng ghi nhận rõ VariantId và Màu sắc
        var orderItem = res.Order.Items[0];
        Assert.Equal(102, orderItem.VariantId);
        Assert.Equal("Đen", orderItem.ColorName);
        Assert.Equal("DELL-1-BLK-16-512", orderItem.SKU);

        // 3. Nghiệp vụ trả góp Mốc 1 chuẩn xác:
        // Giá 17.990.000đ -> Trả trước 30% = 5.397.000đ
        Assert.Equal(5397000m, res.Order.PrepayAmount);
        // Tổng số tiền cần thu trước khi xác nhận trả trước là 17.990.000đ (toàn bộ đơn hàng)
        Assert.Equal(17990000m, res.Order.RemainingDebt);
        // Hợp đồng trả góp ghi nhận dư nợ vay 70% = 12.593.000đ
        Assert.Equal(12593000m, res.Order.InstallmentContract!.TotalFinancedAmount);

        // 4. Lịch trả góp 6 kỳ: 5 kỳ đầu 2.098.833đ, kỳ cuối dồn lẻ 2.098.835đ
        var contract = res.Order.InstallmentContract;
        Assert.NotNull(contract);
        Assert.Equal(6, contract.Schedules.Count);

        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(2098833m, contract.Schedules[i].Amount);
        }
        Assert.Equal(2098835m, contract.Schedules[5].Amount);
        Assert.Equal(12593000m, contract.Schedules.Sum(s => s.Amount));
    }

    [Fact]
    public async Task ColorsController_DuplicateColorName_ReturnsValidationError()
    {
        // ARRANGE
        var (db, _, _, _, _, _) = CreateTestSetup();
        var controller = new ColorsController(db);

        var duplicateColor = new Color
        {
            Name = "Bạc", // Đã có trong DB
            HexCode = "#CCCCCC",
            IsActive = true
        };

        // ACT
        var actionResult = await controller.Create(duplicateColor);

        // ASSERT
        var viewResult = Assert.IsType<ViewResult>(actionResult);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey("Name"));
        Assert.Contains("đã tồn tại", controller.ModelState["Name"]!.Errors[0].ErrorMessage);
    }

    private class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object> values) { }
    }

    [Fact]
    public async Task ColorsController_DeleteColorWithExistingVariants_IsPreventedWithWarning()
    {
        // ARRANGE
        var (db, _, silver, _, _, _) = CreateTestSetup();
        var controller = new ColorsController(db)
        {
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), new FakeTempDataProvider())
        };

        // ACT
        // Cố gắng xóa màu Bạc (đang có biến thể vSilver liên kết)
        var actionResult = await controller.DeleteConfirmed(silver.ColorId);

        // ASSERT
        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Index", redirectResult.ActionName);

        // Cảnh báo không được xóa và màu vẫn còn trong DB
        Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
        var colorStillExists = await db.Colors.AnyAsync(c => c.ColorId == silver.ColorId);
        Assert.True(colorStillExists);
    }

    [Fact]
    public async Task OrderWithoutVariantId_ForProductWithActiveVariants_IsRejected()
    {
        // ARRANGE: Sản phẩm Dell đã có phiên bản màu sắc nhưng khách gửi giỏ hàng không có VariantId
        var (db, _, _, _, _, _) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Nguyễn Thiếu Màu",
            PhoneNumber = "0966666666",
            ShippingAddress = "Hà Nội",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = null, // Thiếu VariantId
                    Quantity = 1
                }
            }
        };

        // ACT
        var res = await orderService.CreateOrderAsync(req);

        // ASSERT: Hệ thống từ chối và yêu cầu chọn màu sắc/phiên bản cụ thể
        Assert.False(res.Success);
        Assert.Contains("có nhiều phiên bản màu sắc", res.Message, StringComparison.OrdinalIgnoreCase);

        // Không trừ kho và không tạo đơn hàng
        var orderCount = await db.Orders.CountAsync();
        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task OrderWithoutVariantId_ForLegacyProductWithoutVariants_IsAllowed()
    {
        // ARRANGE: Sản phẩm phụ kiện / sản phẩm cũ chưa chuyển sang cấu hình phiên bản
        var (db, _, _, _, _, _) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var legacyProduct = new Product
        {
            ProductId = 99,
            ProductName = "Chuột không dây Logitech (Legacy)",
            BrandId = 1,
            CategoryId = 1,
            Price = 350000m,
            StockQuantity = 5,
            IsActive = true
        };
        db.Products.Add(legacyProduct);
        await db.SaveChangesAsync();

        var req = new CreateOrderRequest
        {
            CustomerName = "Khách mua phụ kiện",
            PhoneNumber = "0977777777",
            ShippingAddress = "Hà Nội",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 99,
                    VariantId = null, // Không có variant vì sản phẩm cũ
                    Quantity = 2
                }
            }
        };

        // ACT
        var res = await orderService.CreateOrderAsync(req);

        // ASSERT: Thành công và trừ trực tiếp vào Product.StockQuantity (5 -> 3)
        Assert.True(res.Success);
        var freshP = await db.Products.FindAsync(99);
        Assert.NotNull(freshP);
        Assert.Equal(3, freshP.StockQuantity);
    }

    [Fact]
    public async Task ProductTotalStockInconsistency_WhenProductStockIsZero_RollbacksTransactionAndRevertsVariant()
    {
        // ARRANGE: Tình huống dữ liệu kho bị lệch: Biến thể Bạc còn 1 máy nhưng kho tổng Product bị 0
        var (db, product, _, _, _, _) = CreateTestSetup();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        product.StockQuantity = 0; // Giả lập kho tổng bị lệch
        await db.SaveChangesAsync();

        var req = new CreateOrderRequest
        {
            CustomerName = "Đặng Lệch Kho",
            PhoneNumber = "0988888888",
            ShippingAddress = "Hà Nội",
            PaymentMethod = "COD",
            PurchaseMode = "Full",
            Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = 1,
                    VariantId = 101, // Bạc (kho variant còn 1)
                    Quantity = 1
                }
            }
        };

        // ACT
        var res = await orderService.CreateOrderAsync(req);

        // ASSERT: Lệnh trừ kho tổng thất bại -> Rollback và hoàn lại variant đã trừ
        Assert.False(res.Success);
        Assert.Contains("không đủ tồn kho tổng", res.Message, StringComparison.OrdinalIgnoreCase);

        // Tồn kho của variant Bạc vẫn được giữ nguyên = 1
        var freshSilver = await db.ProductVariants.FindAsync(101);
        Assert.NotNull(freshSilver);
        Assert.Equal(1, freshSilver.StockQuantity);

        // Không tạo đơn
        Assert.Equal(0, await db.Orders.CountAsync());
    }

    [Fact]
    public void ProductVariant_EffectivePrice_WhenPriceIsNull_OrZero_InheritsProductPrice()
    {
        var product = new Product
        {
            ProductId = 1,
            ProductName = "Dell Inspiron 15",
            Price = 17990000m,
            DiscountPrice = 16490000m
        };

        // 1. Variant có Price = null -> Kế thừa giá khuyến mãi của Product (16.490.000đ)
        var variantNullPrice = new ProductVariant
        {
            Product = product,
            Price = null,
            DiscountPrice = null
        };
        Assert.Equal(16490000m, variantNullPrice.EffectivePrice);

        // 2. Variant có Price = 0 (lỗi nhập liệu) -> Vẫn fallback an toàn về giá máy, không bao giờ nhận giá 0đ
        var variantZeroPrice = new ProductVariant
        {
            Product = product,
            Price = 0m,
            DiscountPrice = null
        };
        Assert.Equal(16490000m, variantZeroPrice.EffectivePrice);

        // 3. Variant có Price riêng cao hơn (cấu hình 32GB/1TB) -> Dùng giá riêng
        var variantCustomPrice = new ProductVariant
        {
            Product = product,
            Price = 21990000m,
            DiscountPrice = null
        };
        Assert.Equal(21990000m, variantCustomPrice.EffectivePrice);

        // 4. Có CreatedAt tự động khởi tạo
        Assert.True(variantNullPrice.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SqlServer_ColorsIncludeVariants_ExecutesWithoutMissingColumnError()
    {
        // Kiểm tra trực tiếp trên SQL Server thực tế (nếu có kết nối tới SQLEXPRESS)
        string connStr = "Server=.\\SQLEXPRESS;Database=LaptopStoreDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";
        
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connStr)
                .Options;

            using var db = new AppDbContext(options);
            if (!await db.Database.CanConnectAsync())
            {
                return; // Bỏ qua nếu môi trường không có SQLEXPRESS
            }

            // Chạy DbInitializer để kích hoạt migration CreatedAt & Price null
            DbInitializer.Initialize(db);

            // Thực thi đúng câu lệnh từng gây lỗi: Include(c => c.Variants).ToListAsync()
            var colorsWithVariants = await db.Colors
                .Include(c => c.Variants)
                .AsNoTracking()
                .ToListAsync();

            Assert.NotNull(colorsWithVariants);
            Assert.True(colorsWithVariants.Count > 0);

            // Kiểm tra các biến thể load lên không bị lỗi CreatedAt
            var allVariants = colorsWithVariants.SelectMany(c => c.Variants).ToList();
            foreach (var v in allVariants)
            {
                Assert.True(v.CreatedAt > DateTime.MinValue);
            }
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("network-related") || ex.Message.Contains("server was not found"))
        {
            // Bỏ qua nếu SQL Server không reachable trong runner
        }
    }
}
