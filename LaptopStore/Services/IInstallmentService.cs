using LaptopStore.ViewModels;

namespace LaptopStore.Services;

public interface IInstallmentService
{
    
    InstallmentPlanDto CalculatePlan(decimal totalAmount, int termMonths, DateTime? startDate = null);

    Task<InstallmentContractDetailDto?> GetContractByIdAsync(int contractId);

    Task<InstallmentContractDetailDto?> GetContractByOrderIdAsync(int orderId);

    Task<PagedResult<InstallmentContractSummaryDto>> GetContractsAsync(string? search, string? status, int page, int pageSize);

    Task<(bool Success, string Message, InstallmentContractDetailDto? Contract)> ActivateContractAsync(int contractId, string confirmedBy, string paymentMethod, string? transactionRef, string? note);

    Task<(bool Success, string Message, InstallmentScheduleDto? Schedule)> CollectPeriodPaymentAsync(int contractId, int periodNumber, string collectedBy, string paymentMethod, string? transactionRef, string? note);

    Task<(int WaitingPrepay, int ActiveContracts, decimal TotalDebt)> GetInstallmentDashboardMetricsAsync();
}
