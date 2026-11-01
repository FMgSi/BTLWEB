using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.Services;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaptopStore.Tests;

public class InstallmentServiceTest
{
    private static AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new AppDbContext(options);

        // Seed data mẫu
        var brand = new Brand { BrandId = 1, BrandName = "ASUS" };
        var category = new Category { CategoryId = 1, CategoryName = "Laptop gaming" };

        context.Brands.Add(brand);
        context.Categories.Add(category);

        var product = new Product
        {
            ProductId = 1,
            ProductName = "ASUS ROG Strix G16",
            BrandId = 1,
            CategoryId = 1,
            Price = 20000000m,
            DiscountPrice = null,
            StockQuantity = 10,
            IsActive = true
        };

        context.Products.Add(product);
        context.SaveChanges();

        return context;
    }

    [Fact]
    public void CalculatePlan_WithSampleOrder_20Million_6Months_ReturnsExactMath()
    {
        // Nghiệp vụ đơn hàng mẫu: 20.000.000đ, 6 tháng
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);

        var plan = installmentService.CalculatePlan(20000000m, 6);

        // 1. Trả trước 30% = 6.000.000đ
        Assert.Equal(6000000m, plan.PrepayAmount);
        Assert.Equal(30.00m, plan.PrepayPercent);

        // 2. Dư nợ còn lại = 14.000.000đ
        Assert.Equal(14000000m, plan.FinancedAmount);
        Assert.Equal(0.00m, plan.InterestRate);

        // 3. 6 kỳ: 5 kỳ đầu mỗi kỳ 2.333.333đ, kỳ cuối dồn lẻ 2.333.335đ
        Assert.Equal(6, plan.Periods.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(2333333m, plan.Periods[i].Amount);
        }
        Assert.Equal(2333335m, plan.Periods[5].Amount);

        // 4. Tổng tiền trả trước + tất cả các kỳ luôn khớp chính xác 100% đến từng đồng
        decimal totalSum = plan.PrepayAmount + plan.Periods.Sum(p => p.Amount);
        Assert.Equal(20000000m, totalSum);
    }

    [Theory]
    [InlineData(15555555, 3)]
    [InlineData(20000000, 6)]
    [InlineData(25750000, 9)]
    [InlineData(33990000, 12)]
    public void CalculatePlan_AllTerms_AlwaysSumConserving(decimal totalAmount, int termMonths)
    {
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);

        var plan = installmentService.CalculatePlan(totalAmount, termMonths);

        decimal totalSum = plan.PrepayAmount + plan.Periods.Sum(p => p.Amount);
        Assert.Equal(totalAmount, totalSum);
        Assert.Equal(termMonths, plan.Periods.Count);
    }

    [Fact]
    public async Task CreateOrder_WithInstallment_CreatesContractAndSchedulesInSameTransaction()
    {
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Trần Trả Góp",
            PhoneNumber = "0988776655",
            ShippingAddress = "Hà Nội",
            PaymentMethod = "VietQR",
            PurchaseMode = "Installment",
            TermMonths = 6,
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 1 }
            }
        };

        var (success, message, order) = await orderService.CreateOrderAsync(req);

        Assert.True(success);
        Assert.NotNull(order);
        Assert.Equal("Installment", order.PurchaseMode);
        Assert.Equal("Chờ trả trước", order.PaymentStatus);
        Assert.Equal(6000000m, order.PrepayAmount);
        Assert.Equal(20000000m, order.RemainingDebt); // Chưa thu tiền nên nợ nguyên 20tr (hoặc 14tr sau khi thu prepay)

        // Kiểm tra hợp đồng và lịch lưu trong DB
        var savedOrder = await db.Orders
            .Include(o => o.Payments)
            .Include(o => o.InstallmentContract)
                .ThenInclude(c => c!.Schedules)
            .FirstOrDefaultAsync(o => o.OrderCode == order.OrderCode);

        Assert.NotNull(savedOrder);
        Assert.NotNull(savedOrder.InstallmentContract);
        Assert.Equal("Chờ trả trước", savedOrder.InstallmentContract.Status);
        Assert.Equal(6, savedOrder.InstallmentContract.TermMonths);
        Assert.Equal(6, savedOrder.InstallmentContract.Schedules.Count);

        // Khoản Payment ban đầu là Prepay 6.000.000đ
        var prepayPayment = savedOrder.Payments.FirstOrDefault(p => p.PaymentType == "Prepay");
        Assert.NotNull(prepayPayment);
        Assert.Equal(6000000m, prepayPayment.Amount);

        // Tồn kho đã trừ từ 10 xuống 9
        var p = await db.Products.FindAsync(1);
        Assert.Equal(9, p!.StockQuantity);
    }

    [Fact]
    public async Task ActivateContract_ConfirmsPrepay_AndLocksOfficialScheduleDates()
    {
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Lê Kích Hoạt",
            PhoneNumber = "0911223344",
            ShippingAddress = "Đà Nẵng",
            PaymentMethod = "VietQR",
            PurchaseMode = "Installment",
            TermMonths = 6,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        };

        var (_, _, order) = await orderService.CreateOrderAsync(req);
        var contract = await db.InstallmentContracts.FirstOrDefaultAsync(c => c.OrderId == order!.OrderId);

        // Admin kích hoạt hợp đồng (xác nhận đã nhận 6 triệu trả trước)
        var (actSuccess, actMsg, actContract) = await installmentService.ActivateContractAsync(
            contract!.ContractId, "Admin Hoàng", "VietQR", "FT261028-1", "Tiền trả trước đã vào tài khoản MB");

        Assert.True(actSuccess);
        Assert.NotNull(actContract);
        Assert.Equal("Đang trả góp", actContract.Status);
        Assert.Equal("Đã thanh toán", actContract.PrepayStatus);
        Assert.Equal(6000000m, actContract.TotalPaid);
        Assert.Equal(14000000m, actContract.RemainingDebt);

        // Kiểm tra lịch chính thức: kỳ 1 đến hạn sau 1 tháng tính từ ActivatedAt
        var schedule1 = actContract.Schedules.First(s => s.PeriodNumber == 1);
        Assert.Equal(actContract.ActivatedAt!.Value.AddMonths(1).Date, schedule1.DueDate.Date);

        // Chống kích hoạt 2 lần (Idempotency)
        var (retrySuccess, retryMsg, _) = await installmentService.ActivateContractAsync(
            contract.ContractId, "Admin Hoàng", "VietQR", null, null);
        Assert.False(retrySuccess);
        Assert.Contains("không cần kích hoạt lại", retryMsg);
    }

    [Fact]
    public async Task UpdateOrderStatus_DeliveringInstallmentOrder_DoesNotWipeDebt()
    {
        // ĐÚNG THEO NGHIỆP VỤ: Giao máy xong vẫn có thể còn nợ 70%. Giao hàng không làm thay đổi dư nợ!
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Nguyễn Nhận Máy",
            PhoneNumber = "0977665544",
            ShippingAddress = "TP HCM",
            PaymentMethod = "VietQR",
            PurchaseMode = "Installment",
            TermMonths = 6,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        };

        var (_, _, order) = await orderService.CreateOrderAsync(req);
        var contract = await db.InstallmentContracts.FirstOrDefaultAsync(c => c.OrderId == order!.OrderId);

        // 1. Kích hoạt hợp đồng sau khi nhận 6tr
        await installmentService.ActivateContractAsync(contract!.ContractId, "Admin", "VietQR", null, null);

        // 2. Giao máy cho khách (Chuyển đơn sang Hoàn thành / Đang giao)
        var (updateSuccess, _) = await orderService.UpdateOrderStatusAsync(order!.OrderId, "Hoàn thành", "Giao máy xong cho khách", "Admin");
        Assert.True(updateSuccess);

        var freshOrder = await db.Orders
            .Include(o => o.Payments)
            .Include(o => o.InstallmentContract)
            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

        // Trạng thái đơn là Hoàn thành, nhưng trạng thái thanh toán VẪN LÀ "Đang trả góp" (không bị đổi thành Đã thanh toán!)
        Assert.Equal("Hoàn thành", freshOrder!.OrderStatus);
        Assert.Equal("Đang trả góp", freshOrder.PaymentStatus);

        // Dư nợ vẫn giữ nguyên 14.000.000đ
        var freshContractDto = await installmentService.GetContractByIdAsync(contract.ContractId);
        Assert.Equal(14000000m, freshContractDto!.RemainingDebt);
    }

    [Fact]
    public async Task CollectPeriodPayment_Sequential_AntiDoubleCollection_AndCompletion()
    {
        var db = GetInMemoryDbContext();
        var installmentService = new InstallmentService(db);
        var orderService = new OrderService(db, installmentService);

        var req = new CreateOrderRequest
        {
            CustomerName = "Phạm Thu Đủ",
            PhoneNumber = "0944332211",
            ShippingAddress = "Hải Phòng",
            PaymentMethod = "Tiền mặt",
            PurchaseMode = "Installment",
            TermMonths = 3, // Thử 3 tháng cho gọn
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        };

        var (_, _, order) = await orderService.CreateOrderAsync(req);
        var contract = await db.InstallmentContracts.FirstOrDefaultAsync(c => c.OrderId == order!.OrderId);

        // Kích hoạt hợp đồng
        await installmentService.ActivateContractAsync(contract!.ContractId, "Thu ngân", "Tiền mặt", null, null);

        // 1. Thu kỳ 2 trước kỳ 1 -> Phải bị chặn (yêu cầu thu theo thứ tự tuần tự)
        var (skipSuccess, skipMsg, _) = await installmentService.CollectPeriodPaymentAsync(
            contract.ContractId, 2, "Thu ngân", "Tiền mặt", null, null);
        Assert.False(skipSuccess);
        Assert.Contains("theo đúng thứ tự", skipMsg);

        // 2. Thu kỳ 1 -> Thành công
        var (c1Success, _, s1) = await installmentService.CollectPeriodPaymentAsync(
            contract.ContractId, 1, "Thu ngân", "Tiền mặt", null, "Thu kỳ 1");
        Assert.True(c1Success);
        Assert.NotNull(s1);
        Assert.Equal("Đã thanh toán", s1.Status);

        // 3. Thu kỳ 1 lần nữa -> Phải bị chặn (chống thu trùng khi nhân viên bấm 2 lần)
        var (doubleSuccess, doubleMsg, _) = await installmentService.CollectPeriodPaymentAsync(
            contract.ContractId, 1, "Thu ngân", "Tiền mặt", null, null);
        Assert.False(doubleSuccess);
        Assert.Contains("thu trùng bị chặn", doubleMsg);

        // 4. Thu tiếp kỳ 2
        var (c2Success, _, _) = await installmentService.CollectPeriodPaymentAsync(
            contract.ContractId, 2, "Thu ngân", "Tiền mặt", null, "Thu kỳ 2");
        Assert.True(c2Success);

        // 5. Thu đủ kỳ cuối (kỳ 3) -> Hợp đồng chuyển sang "Đã tất toán", dư nợ 0đ
        var (c3Success, _, _) = await installmentService.CollectPeriodPaymentAsync(
            contract.ContractId, 3, "Thu ngân", "Tiền mặt", null, "Thu kỳ 3");
        Assert.True(c3Success);

        var finalContract = await installmentService.GetContractByIdAsync(contract.ContractId);
        Assert.Equal("Đã tất toán", finalContract!.Status);
        Assert.Equal(0m, finalContract.RemainingDebt);
        Assert.Equal(20000000m, finalContract.TotalPaid);

        // Đơn hàng cũng chuyển sang trạng thái "Đã tất toán"
        var finalOrder = await db.Orders.FindAsync(order!.OrderId);
        Assert.Equal("Đã tất toán", finalOrder!.PaymentStatus);
    }
}
