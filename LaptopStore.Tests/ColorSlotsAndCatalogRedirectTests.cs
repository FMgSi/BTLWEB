using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Controllers;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace LaptopStore.Tests;

public class ColorSlotsAndCatalogRedirectTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(opt);
    }

    private class MockWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "test_webroot_" + Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "LaptopStore";
    }

    private IFormFile CreateMockFormFile(string fileName, string content = "fake image bytes")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "Data", fileName);
    }

    [Fact]
    public async Task SuggestModelsAsync_ReturnsMatchingModels_AndDeduplicates()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var brandDell = new Brand { BrandId = 1, BrandName = "Dell" };
        var brandAsus = new Brand { BrandId = 2, BrandName = "ASUS" };
        db.Brands.AddRange(brandDell, brandAsus);

        var p1 = new Product { ProductId = 1, ProductName = "XPS 13 9310", BrandId = 1, Price = 25000000m, IsActive = true };
        var p2 = new Product { ProductId = 2, ProductName = "XPS 13 9320", BrandId = 1, Price = 30000000m, IsActive = true };
        var p3 = new Product { ProductId = 3, ProductName = "ROG Strix G15", BrandId = 2, Price = 22000000m, IsActive = true };
        db.Products.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        var service = new ProductService(db, new MockWebHostEnvironment());

        // Act - Tìm Dell
        var dellResults = await service.SuggestModelsAsync("Dell");
        // Act - Tìm XPS 13
        var xpsResults = await service.SuggestModelsAsync("XPS 13");

        // Assert
        Assert.Equal(2, dellResults.Count);
        Assert.All(dellResults, item => Assert.Equal("Dell", item.Brand));

        Assert.Equal(2, xpsResults.Count);
        Assert.Contains(xpsResults, x => x.ModelName == "XPS 13 9310");
        Assert.Contains(xpsResults, x => x.ModelName == "XPS 13 9320");
    }

    [Fact]
    public async Task ProductImageSlots_AProductHasOnly4ImagesTotal_UpdatingSlot3PreservesOther3Slots()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockEnv = new MockWebHostEnvironment();
        Directory.CreateDirectory(mockEnv.WebRootPath);

        var product = new Product
        {
            ProductId = 10,
            ProductName = "Dell XPS 13",
            Price = 28000000m,
            StockQuantity = 10,
            ThumbnailUrl = "/images/product-slot1.jpg",
            IsActive = true
        };
        db.Products.Add(product);

        // Tạo 4 ảnh ban đầu cho sản phẩm (không phân chia theo màu sắc)
        var productImages = new List<ProductImage>
        {
            new ProductImage { ImageId = 1, ProductId = 10, DisplayOrder = 1, ImageUrl = "/images/slot-1.jpg" },
            new ProductImage { ImageId = 2, ProductId = 10, DisplayOrder = 2, ImageUrl = "/images/slot-2.jpg" },
            new ProductImage { ImageId = 3, ProductId = 10, DisplayOrder = 3, ImageUrl = "/images/slot-3.jpg" },
            new ProductImage { ImageId = 4, ProductId = 10, DisplayOrder = 4, ImageUrl = "/images/slot-4.jpg" }
        };

        db.ProductImages.AddRange(productImages);
        await db.SaveChangesAsync();

        var service = new ProductService(db, mockEnv);

        // Act: Cập nhật thay thế ô ảnh số 3 của sản phẩm
        var editVm = new ProductEditViewModel
        {
            ProductId = 10,
            ProductName = "Dell XPS 13",
            Price = 28000000m,
            StockQuantity = 10,
            Slot3Image = CreateMockFormFile("new-angle3.jpg")
        };

        var updateSuccess = await service.UpdateProductAsync(editVm);
        Assert.True(updateSuccess);

        // Assert: Sản phẩm có đúng 4 ảnh
        var allImages = await db.ProductImages.Where(i => i.ProductId == 10).OrderBy(i => i.DisplayOrder).ToListAsync();
        Assert.Equal(4, allImages.Count);

        // 1. Ô số 3 phải mang URL mới
        var updatedSlot3 = allImages.FirstOrDefault(i => i.DisplayOrder == 3);
        Assert.NotNull(updatedSlot3);
        Assert.StartsWith("/uploads/", updatedSlot3.ImageUrl);
        Assert.EndsWith(".jpg", updatedSlot3.ImageUrl);
        Assert.NotEqual("/images/slot-3.jpg", updatedSlot3.ImageUrl);

        // 2. 3 ảnh còn lại (Ô 1, 2, 4) còn nguyên vẹn 100%
        var slot1 = allImages.First(i => i.DisplayOrder == 1);
        var slot2 = allImages.First(i => i.DisplayOrder == 2);
        var slot4 = allImages.First(i => i.DisplayOrder == 4);
        Assert.Equal("/images/slot-1.jpg", slot1.ImageUrl);
        Assert.Equal("/images/slot-2.jpg", slot2.ImageUrl);
        Assert.Equal("/images/slot-4.jpg", slot4.ImageUrl);
    }

    [Fact]
    public void CatalogController_RedirectsToReactRoutesCorrectly()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"FrontendUrl", "http://localhost:3000"}
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var controller = new CatalogController(configuration: config);

        // Act & Assert
        // 1. /Catalog -> http://localhost:3000/products
        var indexResult = Assert.IsType<RedirectResult>(controller.Index());
        Assert.Equal("http://localhost:3000/products", indexResult.Url);

        // 2. /Catalog/Details/15 -> http://localhost:3000/product/15
        var detailResult = Assert.IsType<RedirectResult>(controller.Details(15));
        Assert.Equal("http://localhost:3000/product/15", detailResult.Url);

        // 3. /Catalog/Lookup -> http://localhost:3000/account
        var lookupResult = Assert.IsType<RedirectResult>(controller.Lookup());
        Assert.Equal("http://localhost:3000/account", lookupResult.Url);
    }
}
