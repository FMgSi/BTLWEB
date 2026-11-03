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
    public async Task ColorSlots_ChangingSlot3OfSilver_OnlyUpdatesSlot3_AndPreservesRemaining7Images()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var mockEnv = new MockWebHostEnvironment();
        Directory.CreateDirectory(mockEnv.WebRootPath);

        var colorSilver = new Color { ColorId = 1, Name = "Bạc", HexCode = "#C0C0C0", IsActive = true };
        var colorBlack = new Color { ColorId = 2, Name = "Đen", HexCode = "#000000", IsActive = true };
        db.Colors.AddRange(colorSilver, colorBlack);

        var product = new Product
        {
            ProductId = 10,
            ProductName = "Dell XPS 13",
            Price = 28000000m,
            StockQuantity = 10,
            ThumbnailUrl = "/images/silver-slot1.jpg",
            IsActive = true
        };
        db.Products.Add(product);

        // Tạo 4 ảnh cho màu Bạc
        var silverImages = new List<ProductImage>
        {
            new ProductImage { ImageId = 1, ProductId = 10, ColorId = 1, DisplayOrder = 1, ImageUrl = "/images/silver-1.jpg" },
            new ProductImage { ImageId = 2, ProductId = 10, ColorId = 1, DisplayOrder = 2, ImageUrl = "/images/silver-2.jpg" },
            new ProductImage { ImageId = 3, ProductId = 10, ColorId = 1, DisplayOrder = 3, ImageUrl = "/images/silver-3.jpg" },
            new ProductImage { ImageId = 4, ProductId = 10, ColorId = 1, DisplayOrder = 4, ImageUrl = "/images/silver-4.jpg" }
        };

        // Tạo 4 ảnh cho màu Đen
        var blackImages = new List<ProductImage>
        {
            new ProductImage { ImageId = 5, ProductId = 10, ColorId = 2, DisplayOrder = 1, ImageUrl = "/images/black-1.jpg" },
            new ProductImage { ImageId = 6, ProductId = 10, ColorId = 2, DisplayOrder = 2, ImageUrl = "/images/black-2.jpg" },
            new ProductImage { ImageId = 7, ProductId = 10, ColorId = 2, DisplayOrder = 3, ImageUrl = "/images/black-3.jpg" },
            new ProductImage { ImageId = 8, ProductId = 10, ColorId = 2, DisplayOrder = 4, ImageUrl = "/images/black-4.jpg" }
        };

        db.ProductImages.AddRange(silverImages);
        db.ProductImages.AddRange(blackImages);
        await db.SaveChangesAsync();

        var service = new ProductService(db, mockEnv);

        // Act: Chỉ thay đổi ô 3 của màu Bạc (ColorId = 1)
        var editVm = new ProductEditViewModel
        {
            ProductId = 10,
            ProductName = "Dell XPS 13",
            Price = 28000000m,
            StockQuantity = 10,
            SelectedColorId = 1, // Màu Bạc
            Slot3Image = CreateMockFormFile("new-silver-angle3.jpg")
        };

        var updateSuccess = await service.UpdateProductAsync(editVm);
        Assert.True(updateSuccess);

        // Assert: Lấy lại toàn bộ 8 ảnh sau khi cập nhật
        var allImages = await db.ProductImages.Where(i => i.ProductId == 10).OrderBy(i => i.ImageId).ToListAsync();
        Assert.Equal(8, allImages.Count);

        // 1. Ô số 3 của màu Bạc phải mang URL mới
        var updatedSilverSlot3 = allImages.FirstOrDefault(i => i.ColorId == 1 && i.DisplayOrder == 3);
        Assert.NotNull(updatedSilverSlot3);
        Assert.StartsWith("/uploads/", updatedSilverSlot3.ImageUrl);
        Assert.EndsWith(".jpg", updatedSilverSlot3.ImageUrl);
        Assert.NotEqual("/images/silver-3.jpg", updatedSilverSlot3.ImageUrl);

        // 2. 7 ảnh còn lại (Ô 1, 2, 4 của Bạc và toàn bộ 4 ô của Đen) phải còn nguyên vẹn 100%
        var silverSlot1 = allImages.First(i => i.ColorId == 1 && i.DisplayOrder == 1);
        var silverSlot2 = allImages.First(i => i.ColorId == 1 && i.DisplayOrder == 2);
        var silverSlot4 = allImages.First(i => i.ColorId == 1 && i.DisplayOrder == 4);
        Assert.Equal("/images/silver-1.jpg", silverSlot1.ImageUrl);
        Assert.Equal("/images/silver-2.jpg", silverSlot2.ImageUrl);
        Assert.Equal("/images/silver-4.jpg", silverSlot4.ImageUrl);

        var blackSlot1 = allImages.First(i => i.ColorId == 2 && i.DisplayOrder == 1);
        var blackSlot2 = allImages.First(i => i.ColorId == 2 && i.DisplayOrder == 2);
        var blackSlot3 = allImages.First(i => i.ColorId == 2 && i.DisplayOrder == 3);
        var blackSlot4 = allImages.First(i => i.ColorId == 2 && i.DisplayOrder == 4);
        Assert.Equal("/images/black-1.jpg", blackSlot1.ImageUrl);
        Assert.Equal("/images/black-2.jpg", blackSlot2.ImageUrl);
        Assert.Equal("/images/black-3.jpg", blackSlot3.ImageUrl);
        Assert.Equal("/images/black-4.jpg", blackSlot4.ImageUrl);
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
