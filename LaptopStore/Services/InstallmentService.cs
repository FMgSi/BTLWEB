using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Services;

public class InstallmentService : IInstallmentService
{
    private readonly AppDbContext _db;

    public InstallmentService(AppDbContext db)
    {
        _db = db;
    }

    public InstallmentPlanDto CalculatePlan(decimal totalAmount, int termMonths, DateTime? startDate = null)
    {
        if (termMonths != 3 && termMonths != 6 && termMonths != 9 && termMonths != 12)
        {
            termMonths = 6;
        }

        const decimal prepayPercent = 30.00m;
        
        decimal prepayAmount = Math.Round(totalAmount * (prepayPercent / 100m), 0, MidpointRounding.AwayFromZero);
        decimal financedAmount = totalAmount - prepayAmount;

        decimal basePeriodAmount = Math.Floor(financedAmount / termMonths);

        decimal lastPeriodAmount = financedAmount - (basePeriodAmount * (termMonths - 1));

        var baseDate = startDate ?? DateTime.UtcNow;
        var periods = new List<InstallmentPeriodPreviewDto>();

        for (int i = 1; i <= termMonths; i++)
        {
            decimal periodAmount = (i == termMonths) ? lastPeriodAmount : basePeriodAmount;
            periods.Add(new InstallmentPeriodPreviewDto
            {
                PeriodNumber = i,
                DueDate = baseDate.AddMonths(i),
                Amount = periodAmount
            });
        }

        return new InstallmentPlanDto
        {
            TotalAmount = totalAmount,
            TermMonths = termMonths,
            PrepayPercent = prepayPercent,
            PrepayAmount = prepayAmount,
            FinancedAmount = financedAmount,
            InterestRate = 0.00m,
            MonthlyInstallmentAmount = basePeriodAmount,
            LastPeriodAmount = lastPeriodAmount,
            Periods = periods
        };
    }

    public async Task<InstallmentContractDetailDto?> GetContractByIdAsync(int contractId)
    {
        var contract = await _db.InstallmentContracts
            .AsNoTracking()
            .Include(c => c.Order)
                .ThenInclude(o => o!.Payments)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.ContractId == contractId);

        return contract != null ? MapToDetailDto(contract) : null;
    }

    public async Task<InstallmentContractDetailDto?> GetContractByOrderIdAsync(int orderId)
    {
        var contract = await _db.InstallmentContracts
            .AsNoTracking()
            .Include(c => c.Order)
                .ThenInclude(o => o!.Payments)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.OrderId == orderId);

        return contract != null ? MapToDetailDto(contract) : null;
    }

    public async Task<PagedResult<InstallmentContractSummaryDto>> GetContractsAsync(string? search, string? status, int page, int pageSize)
    {
        var query = _db.InstallmentContracts
            .AsNoTracking()
            .Include(c => c.Order)
                .ThenInclude(o => o!.Payments)
            .Include(c => c.Schedules)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.ContractCode.ToLower().Contains(s) ||
                (c.Order != null && (
                    c.Order.OrderCode.ToLower().Contains(s) ||
                    c.Order.CustomerName.ToLower().Contains(s) ||
                    c.Order.PhoneNumber.Contains(s)
                ))
            );
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (status.Equals("Chờ trả trước", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == "Chờ trả trước");
            }
            else if (status.Equals("Đang trả góp", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == "Đang trả góp");
            }
            else if (status.Equals("Đã tất toán", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == "Đã tất toán");
            }
            else if (status.Equals("Quá hạn", StringComparison.OrdinalIgnoreCase))
            {
                var now = DateTime.UtcNow;
                query = query.Where(c => c.Status == "Đang trả góp" && c.Schedules.Any(s => s.Status != "Đã thanh toán" && s.DueDate < now));
            }
            else if (status.Equals("Đã hủy", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(c => c.Status == "Đã hủy");
            }
        }

        int totalItems = await query.CountAsync();
        var rawContracts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var nowUtc = DateTime.UtcNow;
        var items = rawContracts.Select(c =>
        {
            var order = c.Order;
            decimal totalOrderAmount = order?.TotalAmount ?? (c.PrepayAmount + c.TotalFinancedAmount);
            decimal totalPaid = order?.Payments?.Where(p => p.PaymentStatus == "Thành công").Sum(p => p.Amount) ?? 0;
            decimal remainingDebt = Math.Max(0, totalOrderAmount - totalPaid);

            int paidCount = c.Schedules.Count(s => s.Status == "Đã thanh toán");
            bool hasOverdue = c.Status == "Đang trả góp" && c.Schedules.Any(s => s.Status != "Đã thanh toán" && s.DueDate < nowUtc);

            bool isPrepayPaid = c.Status != "Chờ trả trước" || (order?.Payments?.Any(p => p.PaymentType == "Prepay" && p.PaymentStatus == "Thành công") ?? false);

            return new InstallmentContractSummaryDto
            {
                ContractId = c.ContractId,
                ContractCode = c.ContractCode,
                OrderId = c.OrderId,
                OrderCode = order?.OrderCode ?? "",
                CustomerName = order?.CustomerName ?? "",
                PhoneNumber = order?.PhoneNumber ?? "",
                TotalOrderAmount = totalOrderAmount,
                TermMonths = c.TermMonths,
                PrepayAmount = c.PrepayAmount,
                PrepayStatus = isPrepayPaid ? "Đã thanh toán" : "Chưa thanh toán",
                TotalPaid = totalPaid,
                RemainingDebt = remainingDebt,
                ProgressText = $"{paidCount}/{c.TermMonths} kỳ",
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                ActivatedAt = c.ActivatedAt,
                HasOverdue = hasOverdue
            };
        }).ToList();

        return new PagedResult<InstallmentContractSummaryDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(bool Success, string Message, InstallmentContractDetailDto? Contract)> ActivateContractAsync(
        int contractId, string confirmedBy, string paymentMethod, string? transactionRef, string? note)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(bool Success, string Message, InstallmentContractDetailDto? Contract)>(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var contract = await _db.InstallmentContracts
                    .Include(c => c.Order)
                        .ThenInclude(o => o!.Payments)
                    .Include(c => c.Schedules)
                    .FirstOrDefaultAsync(c => c.ContractId == contractId);

                if (contract == null)
                {
                    return (false, "Không tìm thấy hợp đồng trả góp.", null);
                }

                if (contract.Status == "Đã hủy")
                {
                    return (false, "Hợp đồng trả góp đã bị hủy, không thể kích hoạt.", null);
                }

                if (contract.Status != "Chờ trả trước")
                {
                    return (false, $"Hợp đồng đã ở trạng thái '{contract.Status}', không cần kích hoạt lại.", MapToDetailDto(contract));
                }

                var order = contract.Order;
                if (order == null)
                {
                    return (false, "Không tìm thấy đơn hàng liên kết với hợp đồng.", null);
                }

                var existingPrepayPayment = order.Payments.FirstOrDefault(p => p.PaymentType == "Prepay");
                if (existingPrepayPayment != null)
                {
                    existingPrepayPayment.PaymentStatus = "Thành công";
                    existingPrepayPayment.PaymentDate = DateTime.UtcNow;
                    existingPrepayPayment.PaymentMethod = paymentMethod;
                    existingPrepayPayment.ConfirmedBy = confirmedBy;
                    existingPrepayPayment.TransactionReference = transactionRef;
                    existingPrepayPayment.Note = note ?? $"Xác nhận thanh toán trả trước 30% ({contract.PrepayAmount:N0}đ) bởi {confirmedBy}";
                }
                else
                {
                    order.Payments.Add(new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentMethod = paymentMethod,
                        Amount = contract.PrepayAmount,
                        PaymentStatus = "Thành công",
                        PaymentDate = DateTime.UtcNow,
                        PaymentType = "Prepay",
                        ConfirmedBy = confirmedBy,
                        TransactionReference = transactionRef,
                        Note = note ?? $"Xác nhận thanh toán trả trước 30% ({contract.PrepayAmount:N0}đ) bởi {confirmedBy}"
                    });
                }

                contract.Status = "Đang trả góp";
                contract.ActivatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(note))
                {
                    contract.Notes = (string.IsNullOrWhiteSpace(contract.Notes) ? "" : contract.Notes + " | ") + note;
                }

                foreach (var schedule in contract.Schedules.OrderBy(s => s.PeriodNumber))
                {
                    schedule.DueDate = contract.ActivatedAt.Value.AddMonths(schedule.PeriodNumber);
                }

                order.PaymentStatus = "Đang trả góp";
                order.UpdatedAt = DateTime.UtcNow;

                order.StatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.OrderId,
                    Status = order.OrderStatus,
                    ChangedBy = confirmedBy,
                    ChangedAt = DateTime.UtcNow,
                    Note = $"Đã xác nhận thanh toán khoản trả trước 30% ({contract.PrepayAmount:N0}đ) và kích hoạt hợp đồng trả góp #{contract.ContractCode}"
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, "Xác nhận tiền trả trước và kích hoạt hợp đồng trả góp thành công!", MapToDetailDto(contract));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "Lỗi khi kích hoạt hợp đồng: " + ex.Message, null);
            }
        });
    }

    public async Task<(bool Success, string Message, InstallmentScheduleDto? Schedule)> CollectPeriodPaymentAsync(
        int contractId, int periodNumber, string collectedBy, string paymentMethod, string? transactionRef, string? note)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<(bool Success, string Message, InstallmentScheduleDto? Schedule)>(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var contract = await _db.InstallmentContracts
                    .Include(c => c.Order)
                        .ThenInclude(o => o!.Payments)
                    .Include(c => c.Schedules)
                    .FirstOrDefaultAsync(c => c.ContractId == contractId);

                if (contract == null)
                {
                    return (false, "Không tìm thấy hợp đồng trả góp.", null);
                }

                if (contract.Status != "Đang trả góp")
                {
                    return (false, $"Hợp đồng đang ở trạng thái '{contract.Status}', không thể thu kỳ.", null);
                }

                var schedule = contract.Schedules.FirstOrDefault(s => s.PeriodNumber == periodNumber);
                if (schedule == null)
                {
                    return (false, $"Không tìm thấy kỳ trả góp số {periodNumber}.", null);
                }

                if (schedule.Status == "Đã thanh toán")
                {
                    return (false, $"Kỳ số {periodNumber} đã được thanh toán trước đó vào lúc {schedule.PaidAt:dd/MM/yyyy HH:mm}. Thao tác thu trùng bị chặn.", MapToScheduleDto(schedule));
                }

                var previousUnpaid = contract.Schedules
                    .Where(s => s.PeriodNumber < periodNumber && s.Status != "Đã thanh toán")
                    .OrderBy(s => s.PeriodNumber)
                    .FirstOrDefault();

                if (previousUnpaid != null)
                {
                    return (false, $"Vui lòng thu tiền kỳ {previousUnpaid.PeriodNumber} trước khi thu kỳ {periodNumber} (yêu cầu thu theo đúng thứ tự).", null);
                }

                var order = contract.Order;
                if (order == null)
                {
                    return (false, "Không tìm thấy đơn hàng tương ứng.", null);
                }

                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = paymentMethod,
                    Amount = schedule.Amount,
                    PaymentStatus = "Thành công",
                    PaymentDate = DateTime.UtcNow,
                    PaymentType = "InstallmentPeriod",
                    ConfirmedBy = collectedBy,
                    TransactionReference = transactionRef,
                    Note = note ?? $"Thu tiền kỳ {periodNumber}/{contract.TermMonths} - HĐ #{contract.ContractCode} ({schedule.Amount:N0}đ)"
                };

                _db.Payments.Add(payment);
                await _db.SaveChangesAsync();

                schedule.Status = "Đã thanh toán";
                schedule.PaidAmount = schedule.Amount;
                schedule.PaidAt = DateTime.UtcNow;
                schedule.CollectedBy = collectedBy;
                schedule.PaymentId = payment.PaymentId;

                payment.ScheduleId = schedule.ScheduleId;

                bool allSchedulesPaid = contract.Schedules.All(s => s.PeriodNumber == periodNumber || s.Status == "Đã thanh toán");

                if (allSchedulesPaid)
                {
                    contract.Status = "Đã tất toán";
                    contract.CompletedAt = DateTime.UtcNow;
                    order.PaymentStatus = "Đã tất toán";

                    order.StatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.OrderId,
                        Status = order.OrderStatus,
                        ChangedBy = collectedBy,
                        ChangedAt = DateTime.UtcNow,
                        Note = $"Hợp đồng trả góp #{contract.ContractCode} đã tất toán thành công toàn bộ {contract.TermMonths} kỳ!"
                    });
                }
                else
                {
                    order.StatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.OrderId,
                        Status = order.OrderStatus,
                        ChangedBy = collectedBy,
                        ChangedAt = DateTime.UtcNow,
                        Note = $"Đã thu tiền kỳ {periodNumber}/{contract.TermMonths} ({schedule.Amount:N0}đ) bởi {collectedBy}"
                    });
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, $"Thu tiền kỳ {periodNumber} thành công ({schedule.Amount:N0}đ)!", MapToScheduleDto(schedule));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "Lỗi khi ghi nhận thu tiền kỳ: " + ex.Message, null);
            }
        });
    }

    public async Task<(int WaitingPrepay, int ActiveContracts, decimal TotalDebt)> GetInstallmentDashboardMetricsAsync()
    {
        var contracts = await _db.InstallmentContracts
            .AsNoTracking()
            .Include(c => c.Order)
                .ThenInclude(o => o!.Payments)
            .ToListAsync();

        int waitingPrepay = contracts.Count(c => c.Status == "Chờ trả trước");
        int activeContracts = contracts.Count(c => c.Status == "Đang trả góp");

        decimal totalDebt = 0;
        foreach (var c in contracts.Where(c => c.Status == "Đang trả góp"))
        {
            var orderTotal = c.Order?.TotalAmount ?? (c.PrepayAmount + c.TotalFinancedAmount);
            var paid = c.Order?.Payments?.Where(p => p.PaymentStatus == "Thành công").Sum(p => p.Amount) ?? 0;
            totalDebt += Math.Max(0, orderTotal - paid);
        }

        return (waitingPrepay, activeContracts, totalDebt);
    }

    private static InstallmentScheduleDto MapToScheduleDto(InstallmentSchedule s)
    {
        var now = DateTime.UtcNow;
        var displayStatus = s.Status;
        if (s.Status != "Đã thanh toán" && s.DueDate < now)
        {
            displayStatus = "Quá hạn";
        }

        return new InstallmentScheduleDto
        {
            ScheduleId = s.ScheduleId,
            PeriodNumber = s.PeriodNumber,
            DueDate = s.DueDate,
            Amount = s.Amount,
            PaidAmount = s.PaidAmount,
            Status = displayStatus,
            PaidAt = s.PaidAt,
            CollectedBy = s.CollectedBy,
            PaymentId = s.PaymentId
        };
    }

    private static InstallmentContractDetailDto MapToDetailDto(InstallmentContract c)
    {
        var order = c.Order;
        decimal totalOrderAmount = order?.TotalAmount ?? (c.PrepayAmount + c.TotalFinancedAmount);
        var successfulPayments = order?.Payments?.Where(p => p.PaymentStatus == "Thành công").ToList() ?? new List<Payment>();
        decimal totalPaid = successfulPayments.Sum(p => p.Amount);
        decimal remainingDebt = Math.Max(0, totalOrderAmount - totalPaid);

        bool isPrepayPaid = c.Status != "Chờ trả trước" || successfulPayments.Any(p => p.PaymentType == "Prepay");

        var schedulesDto = c.Schedules
            .OrderBy(s => s.PeriodNumber)
            .Select(MapToScheduleDto)
            .ToList();

        var nextPeriod = schedulesDto.FirstOrDefault(s => s.Status != "Đã thanh toán");
        if (nextPeriod != null)
        {
            nextPeriod.IsCurrentDue = true;
        }

        int paidCount = schedulesDto.Count(s => s.Status == "Đã thanh toán");
        int overdueCount = schedulesDto.Count(s => s.Status == "Quá hạn");

        var paymentsDto = (order?.Payments ?? new List<Payment>())
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new InstallmentPaymentReceiptDto
            {
                PaymentId = p.PaymentId,
                PaymentType = p.PaymentType,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaymentStatus = p.PaymentStatus,
                PaymentDate = p.PaymentDate,
                ConfirmedBy = p.ConfirmedBy,
                TransactionReference = p.TransactionReference,
                Note = p.Note
            })
            .ToList();

        return new InstallmentContractDetailDto
        {
            ContractId = c.ContractId,
            ContractCode = c.ContractCode,
            OrderId = c.OrderId,
            OrderCode = order?.OrderCode ?? "",
            CustomerName = order?.CustomerName ?? "",
            PhoneNumber = order?.PhoneNumber ?? "",
            ShippingAddress = order?.ShippingAddress,
            TotalOrderAmount = totalOrderAmount,
            TermMonths = c.TermMonths,
            PrepayPercent = c.PrepayPercent,
            PrepayAmount = c.PrepayAmount,
            PrepayStatus = isPrepayPaid ? "Đã thanh toán" : "Chưa thanh toán",
            TotalFinancedAmount = c.TotalFinancedAmount,
            InterestRate = c.InterestRate,
            MonthlyInstallmentAmount = c.MonthlyInstallmentAmount,
            Status = c.Status,
            OrderStatus = order?.OrderStatus ?? "Đang xử lý",
            CreatedAt = c.CreatedAt,
            ActivatedAt = c.ActivatedAt,
            CompletedAt = c.CompletedAt,
            Notes = c.Notes,
            TotalPaid = totalPaid,
            RemainingDebt = remainingDebt,
            PaidPeriodsCount = paidCount,
            OverduePeriodsCount = overdueCount,
            NextPeriodToPay = nextPeriod,
            Schedules = schedulesDto,
            Payments = paymentsDto
        };
    }
}
