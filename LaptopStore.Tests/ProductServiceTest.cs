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
        var orderService = new OrderService(db);

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
        var orderService = new OrderService(db);

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
}