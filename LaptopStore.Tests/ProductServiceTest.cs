using Xunit;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Controllers.Api;
using LaptopStore.ViewModels;
using LaptopStore.Services;
using LaptopStore.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Tests;

public class ProductServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(opt);

        // Seed data giả lập
        var b = new Brand { BrandId = 1, BrandName = "ASUS" };
        var c = new Category { CategoryId = 1, CategoryName = "Laptop gaming" };
        db.Brands.Add(b);
        db.Categories.Add(c);

        db.Products.AddRange(
            new Product { ProductId = 1, ProductName = "Laptop A", BrandId = 1, CategoryId = 1, Price = 15000000, StockQuantity = 10, IsActive = true },
            new Product { ProductId = 2, ProductName = "Laptop B", BrandId = 1, CategoryId = 1, Price = 25000000, StockQuantity = 5, IsActive = true },
            new Product { ProductId = 3, ProductName = "Laptop C", BrandId = 1, CategoryId = 1, Price = 35000000, StockQuantity = 2, IsActive = true }
        );

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GetProducts_FilteringByPriceRange_ReturnsCorrectCount()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var api = new ProductsApiController(db);

        // Act: lọc sản phẩm từ 10 đến 20 triệu
        var actionRes = await api.GetProducts(q: null, tag: null, brands: null, sizes: null, cpus: null, rams: null, priceRange: "10to20", minPrice: null, maxPrice: null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionRes);
        var pagedResult = Assert.IsType<PagedResult<ProductApiDto>>(okResult.Value);

        Assert.Single(pagedResult.Items);
        Assert.Equal("Laptop A", pagedResult.Items[0].Name);
    }

    [Fact]
    public async Task GetProducts_Paging_ReturnsCorrectPageSize()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var api = new ProductsApiController(db);

        // Act: lấy trang 1, kích thước 2 sản phẩm
        var actionRes = await api.GetProducts(q: null, tag: null, brands: null, sizes: null, cpus: null, rams: null, priceRange: null, minPrice: null, maxPrice: null, sortBy: "newest", page: 1, pageSize: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionRes);
        var pagedResult = Assert.IsType<PagedResult<ProductApiDto>>(okResult.Value);

        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(3, pagedResult.TotalItems);
    }

    [Fact]
    public async Task CreateOrder_DecrementsStock_AndCreatesOrderRecord()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Nguyễn Văn Test",
            PhoneNumber = "0912345678",
            ShippingAddress = "123 Test Street, Hanoi",
            PaymentMethod = "COD",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 2, SelectedSpecs = "RAM 16GB" }
            }
        };

        // Act
        var result = await orderService.CreateOrderAsync(req, userId: null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        Assert.StartsWith("PC", result.Order.OrderCode);
        Assert.Equal(30000000m, result.Order.TotalAmount);

        // Kiểm tra tồn kho đã bị trừ từ 10 xuống 8
        var updatedProduct = await db.Products.FindAsync(1);
        Assert.NotNull(updatedProduct);
        Assert.Equal(8, updatedProduct.StockQuantity);

        // Kiểm tra đơn hàng đã lưu trong database
        var savedOrder = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.OrderCode == result.Order.OrderCode);
        Assert.NotNull(savedOrder);
        Assert.Single(savedOrder.OrderItems);
        Assert.Equal(15000000m, savedOrder.OrderItems.First().UnitPrice);
    }

    [Fact]
    public async Task CreateOrder_ExceedingStock_ReturnsFailureAndDoesNotChangeStock()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Khách Đặt Quá Kho",
            PhoneNumber = "0999888777",
            ShippingAddress = "Da Nang",
            PaymentMethod = "VietQR",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 3, Quantity = 10 } // Tồn kho chỉ có 2
            }
        };

        // Act
        var result = await orderService.CreateOrderAsync(req);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("không đủ tồn kho", result.Message);

        var prod = await db.Products.FindAsync(3);
        Assert.NotNull(prod);
        Assert.Equal(2, prod.StockQuantity); // Tồn kho không bị thay đổi
    }

    [Fact]
    public void PasswordHelper_Pbkdf2HashAndVerify_WorksCorrectly()
    {
        // Arrange
        string rawPassword = "SecurePassword@2026!";

        // Act
        string hash = PasswordHelper.HashPassword(rawPassword);

        // Assert
        Assert.StartsWith("PBKDF2$", hash);
        Assert.True(PasswordHelper.VerifyPassword(rawPassword, hash));
        Assert.False(PasswordHelper.VerifyPassword("WrongPassword", hash));
    }

    [Fact]
    public void PasswordHelper_LegacySha256Verify_Compatible()
    {
        // Arrange
        string rawPassword = "Admin@123";
        string legacyHash = PasswordHelper.HashLegacyPassword(rawPassword);

        // Act & Assert
        Assert.True(PasswordHelper.VerifyPassword(rawPassword, legacyHash));
        Assert.False(PasswordHelper.VerifyPassword("WrongPassword", legacyHash));
    }

    [Fact]
    public async Task CreateProduct_WithStandardDropdownSpecs_SavesCorrectRamAndStorage()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var productService = new ProductService(db, null!);

        var vm = new ProductEditViewModel
        {
            ProductName = "Asus ROG Zephyrus G16",
            BrandId = 1,
            CategoryId = 1,
            Price = 45000000,
            StockQuantity = 5,
            CPU = "Intel Core i9-14900HX",
            GPU = "NVIDIA RTX 4080",
            RamGB = 32,
            StorageGB = 1024,
            StorageType = "SSD NVMe"
        };

        // Act
        var result = await productService.CreateProductAsync(vm);

        // Assert
        Assert.True(result);
        var created = await db.Products.Include(p => p.Specification).FirstOrDefaultAsync(p => p.ProductName == "Asus ROG Zephyrus G16");
        Assert.NotNull(created);
        Assert.NotNull(created.Specification);
        Assert.Equal(32, created.Specification.RamGB);
        Assert.Equal(1024, created.Specification.StorageGB);
        Assert.Equal("SSD NVMe", created.Specification.StorageType);
    }

    [Fact]
    public async Task CreateOrder_WithIdempotencyKey_ReturnsSameOrderWithoutDeductingStockTwice()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Nguyễn Văn Trùng Lặp",
            PhoneNumber = "0987654321",
            ShippingAddress = "Hà Nội",
            PaymentMethod = "COD",
            IdempotencyKey = "TEST-IDEMP-KEY-999",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        // Act 1: Lần gọi thứ nhất
        var firstRes = await orderService.CreateOrderAsync(req);
        Assert.True(firstRes.Success);
        Assert.NotNull(firstRes.Order);
        string orderCode = firstRes.Order.OrderCode;

        var prodAfterFirst = await db.Products.FindAsync(1);
        Assert.Equal(9, prodAfterFirst!.StockQuantity); // Ban đầu 10, trừ 1 còn 9

        // Act 2: Lần gọi thứ hai với CÙNG IdempotencyKey (mạng bị ngắt hoặc khách bấm lại)
        var secondRes = await orderService.CreateOrderAsync(req);

        // Assert 2: Phải trả về cùng mã đơn và KHÔNG trừ kho thêm
        Assert.True(secondRes.Success);
        Assert.NotNull(secondRes.Order);
        Assert.Equal(orderCode, secondRes.Order.OrderCode);

        var prodAfterSecond = await db.Products.FindAsync(1);
        Assert.Equal(9, prodAfterSecond!.StockQuantity); // Vẫn là 9, không bị trừ thêm lần 2!
    }

    [Theory]
    [InlineData("123", false)]
    [InlineData("abcdefghij", false)]
    [InlineData("0123456789", false)] // đầu số 01 cũ không còn hợp lệ
    [InlineData("0912345678", true)]  // đầu số 09 chuẩn 10 số
    [InlineData("0388776655", true)]  // đầu số 03 chuẩn 10 số
    [InlineData("+84988776655", true)] // +84 chuẩn 10 số
    public void CreateOrderRequest_PhoneNumberValidation_EnforcesVietnamFormat(string phone, bool expectedValid)
    {
        var model = new CreateOrderRequest
        {
            CustomerName = "Người Nhận",
            PhoneNumber = phone,
            ShippingAddress = "123 Đường ABC, Hà Nội",
            PaymentMethod = "COD",
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        };

        var context = new System.ComponentModel.DataAnnotations.ValidationContext(model);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
    }
}