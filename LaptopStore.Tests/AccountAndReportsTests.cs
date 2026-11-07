using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaptopStore.Controllers;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaptopStore.Tests;

public class AccountAndReportsTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(opt);
    }

    [Fact]
    public async Task GetCustomerTicketsAsync_ReturnsCustomerTickets_WhenPhoneNumberMatches()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var ticket = new ServiceTicket
        {
            TicketId = 1,
            TicketCode = "TC-20261103-0001",
            TicketType = "Bảo hành",
            CustomerName = "Nguyễn Văn B",
            PhoneNumber = "0912345678",
            FaultDescription = "Máy bật không lên nguồn",
            Status = "Chờ tiếp nhận",
            CreatedAt = DateTime.UtcNow
        };
        db.ServiceTickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new WarrantyService(db);

        // Act
        var result = await service.GetCustomerTicketsAsync("0912345678");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("TC-20261103-0001", result[0].TicketCode);
        Assert.Equal("Chờ tiếp nhận", result[0].Status);
    }

    [Fact]
    public void CatalogPromotions_RedirectsToCatalogIndex()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var controller = new CatalogController(null!, null!, null!, db);

        // Act
        var result = controller.Promotions();

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(CatalogController.Index), redirect.ActionName);
    }

    [Fact]
    public void AdminDiscounts_RedirectsToReports()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var controller = new AdminController(db, null!, null!, null!, null!, null!, null!);

        // Act
        var result = controller.Discounts();

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Reports), redirect.ActionName);
    }

    [Theory]
    [InlineData(15000000, "Đồng", "Bạc", 5000000)]
    [InlineData(25000000, "Bạc", "Vàng", 25000000)]
    [InlineData(70000000, "Vàng", "Kim Cương", 30000000)]
    [InlineData(120000000, "Kim Cương", "Tối Đa", 0)]
    public void MembershipTier_CalculatesCorrectly_BasedOnTotalSpending(
        decimal totalSpent, string expectedTier, string expectedNextTier, decimal expectedRemaining)
    {
        // Act
        string memberTier;
        string nextTier;
        decimal remaining;

        if (totalSpent >= 100000000m)
        {
            memberTier = "Kim Cương";
            nextTier = "Tối Đa";
            remaining = 0;
        }
        else if (totalSpent >= 50000000m)
        {
            memberTier = "Vàng";
            nextTier = "Kim Cương";
            remaining = 100000000m - totalSpent;
        }
        else if (totalSpent >= 20000000m)
        {
            memberTier = "Bạc";
            nextTier = "Vàng";
            remaining = 50000000m - totalSpent;
        }
        else
        {
            memberTier = "Đồng";
            nextTier = "Bạc";
            remaining = 20000000m - totalSpent;
        }

        // Assert
        Assert.Equal(expectedTier, memberTier);
        Assert.Equal(expectedNextTier, nextTier);
        Assert.Equal(expectedRemaining, remaining);
    }

    [Fact]
    public async Task ToggleCustomerLock_TogglesActiveStateSuccessfully()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var user = new User
        {
            UserId = 2,
            Username = "khachhang02",
            FullName = "Nguyễn Văn A",
            Email = "khachhang@gmail.com",
            PhoneNumber = "0912345678",
            PasswordHash = "hash",
            Role = "Customer",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var controller = new AdminController(db, null!, null!, null!, null!, null!, null!);

        // Act 1: Khóa tài khoản
        var res1 = await controller.ToggleCustomerLock("KH002");
        var json1 = Assert.IsType<JsonResult>(res1);
        var jsonDoc1 = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(json1.Value));
        Assert.True(jsonDoc1.RootElement.GetProperty("success").GetBoolean());
        Assert.False(jsonDoc1.RootElement.GetProperty("isActive").GetBoolean());

        var updatedUser1 = await db.Users.FindAsync(2);
        Assert.False(updatedUser1!.IsActive);

        // Act 2: Mở khóa lại
        var res2 = await controller.ToggleCustomerLock("KH002");
        var json2 = Assert.IsType<JsonResult>(res2);
        var jsonDoc2 = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(json2.Value));
        Assert.True(jsonDoc2.RootElement.GetProperty("success").GetBoolean());
        Assert.True(jsonDoc2.RootElement.GetProperty("isActive").GetBoolean());

        var updatedUser2 = await db.Users.FindAsync(2);
        Assert.True(updatedUser2!.IsActive);
    }

    [Fact]
    public async Task Customers_FiltersByTier_Correctly_WithUnaccentedInput()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var user1 = new User
        {
            UserId = 1,
            Username = "user1",
            FullName = "Khách Hàng Đồng",
            Email = "dong@gmail.com",
            PhoneNumber = "0900000001",
            PasswordHash = "h",
            Role = "Customer",
            IsActive = true
        };
        var user2 = new User
        {
            UserId = 2,
            Username = "user2",
            FullName = "Khách Hàng Kim Cương",
            Email = "diamond@gmail.com",
            PhoneNumber = "0900000002",
            PasswordHash = "h",
            Role = "Customer",
            IsActive = true
        };
        db.Users.AddRange(user1, user2);

        // Order > 100M cho user2
        var order = new Order
        {
            OrderId = 1,
            UserId = 2,
            OrderCode = "DH-100",
            CustomerName = "Khách Hàng Kim Cương",
            PhoneNumber = "0900000002",
            ShippingAddress = "Hà Nội",
            TotalAmount = 120_000_000m,
            PaymentStatus = "Đã thanh toán",
            OrderStatus = "Đã giao hàng"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var controller = new AdminController(db, null!, null!, null!, null!, null!, null!);

        // Act & Assert 1: Lọc tier="dong" -> user1
        var viewResult1 = Assert.IsType<ViewResult>(await controller.Customers(null, null, "dong"));
        var list1 = Assert.IsAssignableFrom<List<CustomerDto>>(viewResult1.Model);
        Assert.Single(list1);
        Assert.Equal("KH001", list1[0].CustomerCode);
        Assert.Equal("Đồng", list1[0].MembershipTier);

        // Act & Assert 2: Lọc tier="kimcuong" -> user2
        var viewResult2 = Assert.IsType<ViewResult>(await controller.Customers(null, null, "kimcuong"));
        var list2 = Assert.IsAssignableFrom<List<CustomerDto>>(viewResult2.Model);
        Assert.Single(list2);
        Assert.Equal("KH002", list2[0].CustomerCode);
        Assert.Equal("Kim cương", list2[0].MembershipTier);

        // Act & Assert 3: Lọc tier="all" -> cả 2 user
        var viewResult3 = Assert.IsType<ViewResult>(await controller.Customers(null, null, "all"));
        var list3 = Assert.IsAssignableFrom<List<CustomerDto>>(viewResult3.Model);
        Assert.Equal(2, list3.Count);
    }
}
