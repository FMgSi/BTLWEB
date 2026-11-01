using Xunit;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;

namespace LaptopStore.Tests;

public class WarrantyServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(opt);

        // Seed data
        var b = new Brand { BrandId = 1, BrandName = "DELL" };
        var c = new Category { CategoryId = 1, CategoryName = "Văn phòng" };
        var prod = new Product { ProductId = 1, ProductName = "Dell Inspiron 14", BrandId = 1, CategoryId = 1, Price = 18000000, StockQuantity = 10, IsActive = true };

        db.Brands.Add(b);
        db.Categories.Add(c);
        db.Products.Add(prod);

        // Seed sample service catalog items
        db.ServiceCatalog.AddRange(
            new ServiceCatalogItem { ServiceId = 1, ServiceCode = "DV-SC01", ServiceName = "Vệ sinh laptop", ServiceType = "Sửa chữa", EstimatedCost = 150000, ServiceWarrantyMonths = 1, IsActive = true },
            new ServiceCatalogItem { ServiceId = 2, ServiceCode = "DV-BH01", ServiceName = "Bảo hành Pin", ServiceType = "Bảo hành", EstimatedCost = 0, ServiceWarrantyMonths = 12, IsActive = true }
        );

        // Seed a sample order with 2 items
        var order = new Order
        {
            OrderId = 1,
            OrderCode = "ORD-TEST-001",
            CustomerName = "Nguyễn Văn A",
            PhoneNumber = "0987654321",
            ShippingAddress = "Hà Nội",
            OrderStatus = "Hoàn thành",
            PaymentStatus = "Đã thanh toán",
            TotalAmount = 36000000,
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    OrderItemId = 1,
                    ProductId = 1,
                    ProductName = "Dell Inspiron 14",
                    Quantity = 2,
                    UnitPrice = 18000000,
                    TotalPrice = 36000000
                }
            }
        };

        db.Orders.Add(order);
        db.SaveChanges();

        return db;
    }

    [Fact]
    public async Task AssignSerials_RequiresExactCount_MatchingQuantity()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        // Dòng hàng quantity = 2, nhưng chỉ truyền 1 serial -> phải báo lỗi
        var invalidInput = new List<OrderItemSerialsInput>
        {
            new() { OrderItemId = 1, SerialNumbers = new List<string> { "SN-DELL-001" } }
        };

        // Act
        var (success, message) = await service.AssignSerialsToOrderAsync(1, invalidInput, "AdminTest");

        // Assert
        Assert.False(success);
        Assert.Contains("yêu cầu đúng 2 số Serial", message);
    }

    [Fact]
    public async Task AssignSerials_ValidSerials_SucceedsAndCreatesSoldDevices()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        var validInput = new List<OrderItemSerialsInput>
        {
            new() { OrderItemId = 1, SerialNumbers = new List<string> { "SN-DELL-1001", "SN-DELL-1002" } }
        };

        // Act
        var (success, _) = await service.AssignSerialsToOrderAsync(1, validInput, "AdminTest");

        // Assert
        Assert.True(success);
        var devices = await db.SoldDevices.Where(d => d.OrderItemId == 1).ToListAsync();
        Assert.Equal(2, devices.Count);
        Assert.Contains(devices, d => d.SerialNumber == "SN-DELL-1001");
        Assert.Contains(devices, d => d.SerialNumber == "SN-DELL-1002");
    }

    [Fact]
    public async Task CreateWarrantyTicket_DeliveredAndUnderWarranty_SucceedsWithZeroCost()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        // Tạo 1 máy đã giao còn hạn bảo hành
        var device = new SoldDevice
        {
            DeviceId = 10,
            OrderItemId = 1,
            SerialNumber = "SN-DELL-VALID",
            DeliveredAt = DateTime.UtcNow.AddMonths(-1),
            WarrantyStartDate = DateTime.UtcNow.AddMonths(-1),
            WarrantyEndDate = DateTime.UtcNow.AddMonths(11),
            WarrantyMonths = 12
        };
        db.SoldDevices.Add(device);
        await db.SaveChangesAsync();

        var req = new CreateWarrantyTicketRequest
        {
            SoldDeviceId = 10,
            CustomerName = "Nguyễn Văn A",
            PhoneNumber = "0987654321",
            ServiceId = 2,
            FaultDescription = "Pin bị chai nhanh, dùng 20 phút là sập nguồn"
        };

        // Act
        var (success, _, ticket) = await service.CreateWarrantyTicketAsync(req);

        // Assert
        Assert.True(success);
        Assert.NotNull(ticket);
        Assert.StartsWith("BH-", ticket!.TicketCode);
        Assert.Equal(0, ticket.FinalCost);
        Assert.Equal("Chờ tiếp nhận", ticket.Status);
    }

    [Fact]
    public async Task CreateWarrantyTicket_ExpiredWarranty_RejectsAndAdvisesRepair()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        // Máy đã hết hạn bảo hành từ 1 tháng trước
        var device = new SoldDevice
        {
            DeviceId = 20,
            OrderItemId = 1,
            SerialNumber = "SN-DELL-EXPIRED",
            DeliveredAt = DateTime.UtcNow.AddMonths(-14),
            WarrantyStartDate = DateTime.UtcNow.AddMonths(-14),
            WarrantyEndDate = DateTime.UtcNow.AddMonths(-2),
            WarrantyMonths = 12
        };
        db.SoldDevices.Add(device);
        await db.SaveChangesAsync();

        var req = new CreateWarrantyTicketRequest
        {
            SoldDeviceId = 20,
            CustomerName = "Nguyễn Văn A",
            PhoneNumber = "0987654321",
            FaultDescription = "Máy bật không lên nguồn"
        };

        // Act
        var (success, message, ticket) = await service.CreateWarrantyTicketAsync(req);

        // Assert
        Assert.False(success);
        Assert.Null(ticket);
        Assert.Contains("hết hạn bảo hành", message);
        Assert.Contains("Sửa chữa", message);
    }

    [Fact]
    public async Task CreateWarrantyTicket_DuplicateActiveTicket_RejectsSimultaneousIntake()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        var device = new SoldDevice
        {
            DeviceId = 30,
            OrderItemId = 1,
            SerialNumber = "SN-DELL-ACTIVE",
            DeliveredAt = DateTime.UtcNow.AddMonths(-1),
            WarrantyStartDate = DateTime.UtcNow.AddMonths(-1),
            WarrantyEndDate = DateTime.UtcNow.AddMonths(11),
            WarrantyMonths = 12
        };
        db.SoldDevices.Add(device);

        // Đã có 1 phiếu đang xử lý
        db.ServiceTickets.Add(new ServiceTicket
        {
            TicketCode = "BH-20261101-0099",
            TicketType = "Bảo hành",
            SoldDeviceId = 30,
            CustomerName = "Nguyễn Văn A",
            PhoneNumber = "0987654321",
            FaultDescription = "Lỗi bàn phím",
            Status = "Đang xử lý"
        });
        await db.SaveChangesAsync();

        var req = new CreateWarrantyTicketRequest
        {
            SoldDeviceId = 30,
            CustomerName = "Nguyễn Văn A",
            PhoneNumber = "0987654321",
            FaultDescription = "Lỗi khác muốn tạo tiếp phiếu mới"
        };

        // Act
        var (success, message, _) = await service.CreateWarrantyTicketAsync(req);

        // Assert
        Assert.False(success);
        Assert.Contains("đang có một phiếu dịch vụ", message);
    }

    [Fact]
    public async Task CreateRepairTicket_ExternalDevice_SucceedsForWalkInCustomer()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        var req = new CreateRepairTicketRequest
        {
            DeviceSource = "External",
            ExternalDeviceModel = "MacBook Pro M1 2020",
            ExternalSerialNumber = "C02G1234MD6R",
            CustomerName = "Khách Hàng Mới",
            PhoneNumber = "0912345678",
            ServiceId = 1,
            FaultDescription = "Cần vệ sinh tra keo tản nhiệt định kỳ do máy nóng"
        };

        // Act
        var (success, _, ticket) = await service.CreateRepairTicketAsync(req);

        // Assert
        Assert.True(success);
        Assert.NotNull(ticket);
        Assert.StartsWith("SC-", ticket!.TicketCode);
        Assert.Equal("Sửa chữa", ticket.TicketType);
        Assert.Equal("MacBook Pro M1 2020", ticket.DeviceDisplayName);
    }

    [Fact]
    public async Task SubmitQuoteDecision_Approve_UpdatesStatusToProcessingAndFinalCost()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        var ticket = new ServiceTicket
        {
            TicketId = 100,
            TicketCode = "SC-TEST-QUOTE",
            TicketType = "Sửa chữa",
            CustomerName = "Trần B",
            PhoneNumber = "0933333333",
            FaultDescription = "Vỡ màn hình",
            Status = "Chờ duyệt báo giá",
            Quotes = new List<ServiceQuote>
            {
                new()
                {
                    QuoteId = 1,
                    Description = "Thay màn hình IPS 144Hz",
                    Amount = 1650000,
                    CustomerDecision = "Chờ duyệt"
                }
            }
        };
        db.ServiceTickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act: Khách đồng ý báo giá
        var (success, _) = await service.SubmitQuoteDecisionAsync(100, 1, "Đồng ý", "Đã duyệt giá 1.650.000đ", "0933333333");

        // Assert
        Assert.True(success);
        var updated = await db.ServiceTickets.Include(t => t.Quotes).FirstAsync(t => t.TicketId == 100);
        Assert.Equal("Đang xử lý", updated.Status);
        Assert.Equal(1650000, updated.FinalCost);
        Assert.Equal("Đồng ý", updated.Quotes.First().CustomerDecision);
    }

    [Fact]
    public async Task SubmitQuoteDecision_Reject_UpdatesStatusToWaitingReturn()
    {
        // Arrange
        var db = GetInMemoryDbContext();
        var service = new WarrantyService(db);

        var ticket = new ServiceTicket
        {
            TicketId = 101,
            TicketCode = "SC-TEST-REJECT",
            TicketType = "Sửa chữa",
            CustomerName = "Lê C",
            PhoneNumber = "0944444444",
            FaultDescription = "Hỏng card đồ họa",
            Status = "Chờ duyệt báo giá",
            Quotes = new List<ServiceQuote>
            {
                new()
                {
                    QuoteId = 2,
                    Description = "Đóng chip VGA",
                    Amount = 1250000,
                    CustomerDecision = "Chờ duyệt"
                }
            }
        };
        db.ServiceTickets.Add(ticket);
        await db.SaveChangesAsync();

        // Act: Khách từ chối báo giá
        var (success, _) = await service.SubmitQuoteDecisionAsync(101, 2, "Từ chối", "Giá cao quá, tôi không sửa nữa", "0944444444");

        // Assert
        Assert.True(success);
        var updated = await db.ServiceTickets.Include(t => t.Quotes).FirstAsync(t => t.TicketId == 101);
        Assert.Equal("Chờ trả máy", updated.Status);
        Assert.Equal("Từ chối", updated.Quotes.First().CustomerDecision);
    }
}

