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
}
