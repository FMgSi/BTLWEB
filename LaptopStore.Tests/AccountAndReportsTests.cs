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

    [Fact]
    public async Task CancelOrDeleteOrderAsync_Succeeds_AndRestoresStock_WhenOrderIsUnverified()
    {
        using var db = GetInMemoryDbContext();
        var product = new Product
        {
            ProductId = 10,
            ProductName = "Test Laptop",
            Price = 20_000_000m,
            StockQuantity = 5
        };
        var variant = new ProductVariant
        {
            VariantId = 100,
            ProductId = 10,
            SKU = "TEST-SKU",
            StockQuantity = 2
        };
        db.Products.Add(product);
        db.ProductVariants.Add(variant);

        var order = new Order
        {
            OrderId = 50,
            UserId = 1,
            OrderCode = "PC-TEST-001",
            CustomerName = "Nguyễn Văn Test",
            PhoneNumber = "0988888888",
            ShippingAddress = "Hà Nội",
            TotalAmount = 20_000_000m,
            OrderStatus = "Chờ xác nhận", // Chưa xác minh
            PaymentStatus = "Chờ thanh toán",
            OrderItems = new List<OrderItem>
            {
                new() { OrderItemId = 1, ProductId = 10, VariantId = 100, Quantity = 2, UnitPrice = 10_000_000m }
            }
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db, null!);

        // Act: Khách hàng xóa đơn hàng chưa xác minh
        var (success, message) = await orderService.CancelOrDeleteOrderAsync(50, userId: 1);

        // Assert
        Assert.True(success);
        Assert.Contains("Đã xóa thành công", message);

        // Đơn hàng đã bị xóa khỏi database
        var deletedOrder = await db.Orders.FindAsync(50);
        Assert.Null(deletedOrder);

        // Tồn kho được hoàn trả
        var updatedVariant = await db.ProductVariants.FindAsync(100);
        Assert.Equal(4, updatedVariant!.StockQuantity); // 2 ban đầu + 2 hoàn trả

        var updatedProduct = await db.Products.FindAsync(10);
        Assert.Equal(7, updatedProduct!.StockQuantity); // 5 ban đầu + 2 hoàn trả
    }

    [Fact]
    public async Task CancelOrDeleteOrderAsync_Fails_WhenOrderIsAlreadyVerifiedByStore()
    {
        using var db = GetInMemoryDbContext();
        var order = new Order
        {
            OrderId = 51,
            UserId = 1,
            OrderCode = "PC-TEST-002",
            CustomerName = "Nguyễn Văn Test",
            PhoneNumber = "0988888888",
            ShippingAddress = "Hà Nội",
            TotalAmount = 20_000_000m,
            OrderStatus = "Đã xác nhận", // Đã được cửa hàng xác minh
            PaymentStatus = "Đã thanh toán"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db, null!);

        // Act: Khách hàng cố xóa đơn hàng đã xác minh
        var (success, message) = await orderService.CancelOrDeleteOrderAsync(51, userId: 1);

        // Assert
        Assert.False(success);
        Assert.Contains("Đơn hàng đã được cửa hàng xác minh", message);

        // Đơn hàng vẫn còn nguyên trong database
        var existingOrder = await db.Orders.FindAsync(51);
        Assert.NotNull(existingOrder);
    }

    [Fact]
    public async Task CancelOrDeleteOrderAsync_Succeeds_ForInstallmentOrder_WithMutualPaymentScheduleReferences()
    {
        using var db = GetInMemoryDbContext();
        var product = new Product
        {
            ProductId = 20,
            ProductName = "Surface Laptop Studio 2",
            Price = 50_000_000m,
            StockQuantity = 3
        };
        var variant = new ProductVariant
        {
            VariantId = 200,
            ProductId = 20,
            SKU = "SURFACE-STUDIO-2",
            StockQuantity = 1
        };
        db.Products.Add(product);
        db.ProductVariants.Add(variant);

        var order = new Order
        {
            OrderId = 60,
            UserId = 2,
            OrderCode = "PC-INSTALLMENT-001",
            CustomerName = "Trần Thị Test",
            PhoneNumber = "0977777777",
            ShippingAddress = "TP.HCM",
            TotalAmount = 50_000_000m,
            OrderStatus = "Đang xử lý", // Chưa xác minh
            PaymentStatus = "Chờ trả trước",
            PurchaseMode = "Installment",
            OrderItems = new List<OrderItem>
            {
                new() { OrderItemId = 10, ProductId = 20, VariantId = 200, Quantity = 1, UnitPrice = 50_000_000m }
            }
        };

        var contract = new InstallmentContract
        {
            ContractId = 1,
            OrderId = 60,
            ContractCode = "HDTG-TEST",
            TermMonths = 6,
            PrepayAmount = 15_000_000m,
            TotalFinancedAmount = 35_000_000m,
            MonthlyInstallmentAmount = 5_833_333m,
            Status = "Chờ trả trước"
        };

        var schedule = new InstallmentSchedule
        {
            ScheduleId = 1,
            ContractId = 1,
            PeriodNumber = 1,
            DueDate = DateTime.UtcNow.AddMonths(1),
            Amount = 5_833_333m,
            Status = "Chưa thanh toán"
        };

        var payment = new Payment
        {
            PaymentId = 1,
            OrderId = 60,
            PaymentMethod = "VietQR",
            Amount = 15_000_000m,
            PaymentStatus = "Chờ xác nhận",
            PaymentType = "Prepay"
        };

        // Gắn liên kết 2 chiều giữa Schedule và Payment (nguyên nhân gây lỗi circular dependency trước đây)
        schedule.PaymentId = 1;
        payment.ScheduleId = 1;

        contract.Schedules.Add(schedule);
        order.InstallmentContract = contract;
        order.Payments.Add(payment);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var orderService = new OrderService(db, null!);

        // Act: Khách hàng xóa đơn hàng trả góp chưa xác minh
        var (success, message) = await orderService.CancelOrDeleteOrderAsync(60, userId: 2);

        // Assert
        Assert.True(success);
        Assert.Contains("Đã xóa thành công", message);

        // Kiểm tra xóa sạch các bảng liên quan và không bị lỗi circular dependency
        Assert.Null(await db.Orders.FindAsync(60));
        Assert.Null(await db.InstallmentContracts.FindAsync(1));
        Assert.Null(await db.InstallmentSchedules.FindAsync(1));
        Assert.Null(await db.Payments.FindAsync(1));

        // Tồn kho được phục hồi chính xác
        var updatedVariant = await db.ProductVariants.FindAsync(200);
        Assert.Equal(2, updatedVariant!.StockQuantity); // 1 ban đầu + 1 hoàn trả
        var updatedProduct = await db.Products.FindAsync(20);
        Assert.Equal(4, updatedProduct!.StockQuantity); // 3 ban đầu + 1 hoàn trả
    }
}
