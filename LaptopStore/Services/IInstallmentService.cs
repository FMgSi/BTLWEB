using LaptopStore.ViewModels;

namespace LaptopStore.Services;

public interface IInstallmentService
{
    /// <summary>
    /// Tính toán phương án trả góp: 30% trả trước, chia đều các kỳ, dồn phần lẻ chênh lệch vào kỳ cuối
    /// </summary>
    InstallmentPlanDto CalculatePlan(decimal totalAmount, int termMonths, DateTime? startDate = null);

    /// <summary>
    /// Lấy chi tiết hợp đồng trả góp theo ContractId
    /// </summary>
    Task<InstallmentContractDetailDto?> GetContractByIdAsync(int contractId);

    /// <summary>
    /// Lấy chi tiết hợp đồng trả góp theo OrderId
    /// </summary>
    Task<InstallmentContractDetailDto?> GetContractByOrderIdAsync(int orderId);

    /// <summary>
    /// Lấy danh sách hợp đồng trả góp có phân trang và bộ lọc trạng thái/tìm kiếm
    /// </summary>
    Task<PagedResult<InstallmentContractSummaryDto>> GetContractsAsync(string? search, string? status, int page, int pageSize);

    /// <summary>
    /// Admin xác nhận đã nhận khoản tiền trả trước 30% -> Kích hoạt hợp đồng và chốt lịch trả chính thức
    /// </summary>
    Task<(bool Success, string Message, InstallmentContractDetailDto? Contract)> ActivateContractAsync(int contractId, string confirmedBy, string paymentMethod, string? transactionRef, string? note);

    /// <summary>
    /// Admin thu tiền cho từng kỳ trả góp theo thứ tự
    /// </summary>
    Task<(bool Success, string Message, InstallmentScheduleDto? Schedule)> CollectPeriodPaymentAsync(int contractId, int periodNumber, string collectedBy, string paymentMethod, string? transactionRef, string? note);

    /// <summary>
    /// Lấy số liệu thống kê trả góp cho Dashboard: số đơn chờ trả trước, số hợp đồng đang trả, tổng dư nợ
    /// </summary>
    Task<(int WaitingPrepay, int ActiveContracts, decimal TotalDebt)> GetInstallmentDashboardMetricsAsync();
}
