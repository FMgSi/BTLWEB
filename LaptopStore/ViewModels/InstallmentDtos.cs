using System.ComponentModel.DataAnnotations;

namespace LaptopStore.ViewModels;

public class InstallmentPlanPreviewRequest
{
    [Required]
    [Range(1000000, 1000000000, ErrorMessage = "Tổng giá trị đơn hàng từ 1.000.000đ trở lên để trả góp")]
    public decimal TotalAmount { get; set; }

    [Required]
    [Range(3, 12, ErrorMessage = "Kỳ hạn trả góp phải là 3, 6, 9 hoặc 12 tháng")]
    public int TermMonths { get; set; } = 6;
}

public class InstallmentPlanDto
{
    public decimal TotalAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal PrepayPercent { get; set; } = 30.00m;
    public decimal PrepayAmount { get; set; }
    public decimal FinancedAmount { get; set; }
    public decimal InterestRate { get; set; } = 0.00m;
    public decimal MonthlyInstallmentAmount { get; set; }
    public decimal LastPeriodAmount { get; set; }
    public List<InstallmentPeriodPreviewDto> Periods { get; set; } = new();
}

public class InstallmentPeriodPreviewDto
{
    public int PeriodNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
}

public class InstallmentContractDetailDto
{
    public int ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? ShippingAddress { get; set; }
    public decimal TotalOrderAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal PrepayPercent { get; set; }
    public decimal PrepayAmount { get; set; }
    public string PrepayStatus { get; set; } = "Chưa thanh toán"; 
    public decimal TotalFinancedAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyInstallmentAmount { get; set; }
    public string Status { get; set; } = string.Empty; 
    public string OrderStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal RemainingDebt { get; set; }

    public int PaidPeriodsCount { get; set; }
    public int OverduePeriodsCount { get; set; }

    public InstallmentScheduleDto? NextPeriodToPay { get; set; }
    public List<InstallmentScheduleDto> Schedules { get; set; } = new();
    public List<InstallmentPaymentReceiptDto> Payments { get; set; } = new();
}

public class InstallmentScheduleDto
{
    public int ScheduleId { get; set; }
    public int PeriodNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = "Chưa thanh toán"; 
    public DateTime? PaidAt { get; set; }
    public string? CollectedBy { get; set; }
    public int? PaymentId { get; set; }
    public bool IsCurrentDue { get; set; }
}

public class InstallmentPaymentReceiptDto
{
    public int PaymentId { get; set; }
    public string PaymentType { get; set; } = string.Empty; 
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public string? TransactionReference { get; set; }
    public string? Note { get; set; }
}

public class InstallmentContractSummaryDto
{
    public int ContractId { get; set; }
    public string ContractCode { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal TotalOrderAmount { get; set; }
    public int TermMonths { get; set; }
    public decimal PrepayAmount { get; set; }
    public string PrepayStatus { get; set; } = "Chưa thanh toán";
    public decimal TotalPaid { get; set; }
    public decimal RemainingDebt { get; set; }
    public string ProgressText { get; set; } = string.Empty; 
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public bool HasOverdue { get; set; }
}

public class ActivateContractRequest
{
    [Required]
    public int ContractId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Tiền mặt"; 

    public string? TransactionReference { get; set; }
    public string? Note { get; set; }
}

public class CollectPeriodPaymentRequest
{
    [Required]
    public int ContractId { get; set; }

    [Required]
    public int PeriodNumber { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "Tiền mặt"; 

    public string? TransactionReference { get; set; }
    public string? Note { get; set; }
}
