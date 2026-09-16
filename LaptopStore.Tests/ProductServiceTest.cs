using Xunit;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Controllers.Api;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Tests;

public class ProductServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(opt);

        // Seed data giả lập
        var b = new Brand { BrandId = 1, BrandName = "ASUS" };
        var c = new Category { CategoryId = 1, CategoryName = "Laptop gaming" };
        db.Brands.Add(b);
        db.Categories.Add(c);

        db.Products.AddRange(
            new Product { ProductId = 1, ProductName = "Laptop A", BrandId = 1, CategoryId = 1, Price = 15000000, IsActive = true },
            new Product { ProductId = 2, ProductName = "Laptop B", BrandId = 1, CategoryId = 1, Price = 25000000, IsActive = true },
            new Product { ProductId = 3, ProductName = "Laptop C", BrandId = 1, CategoryId = 1, Price = 35000000, IsActive = true }
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
        var actionRes = await api.GetProducts(tag: null, brands: null, sizes: null, priceRange: "10to20", minPrice: null, maxPrice: null);

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
        var actionRes = await api.GetProducts(tag: null, brands: null, sizes: null, priceRange: null, minPrice: null, maxPrice: null, page: 1, pageSize: 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionRes);
        var pagedResult = Assert.IsType<PagedResult<ProductApiDto>>(okResult.Value);

        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal(3, pagedResult.TotalItems);
    }
}