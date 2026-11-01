using LaptopStore.ViewModels;

namespace LaptopStore.Services;

public interface IWarrantyService
{
    Task<List<ServiceCatalogDto>> GetCatalogAsync(string? type = null);
    Task<List<SoldDeviceDto>> GetCustomerDevicesAsync(string? phoneNumber = null, int? userId = null);
    Task<SoldDeviceDto?> GetDeviceBySerialAsync(string serialNumber);
    Task<(bool Success, string Message, ServiceTicketDetailDto? Ticket)> CreateWarrantyTicketAsync(CreateWarrantyTicketRequest request, int? userId = null);
    Task<(bool Success, string Message, ServiceTicketDetailDto? Ticket)> CreateRepairTicketAsync(CreateRepairTicketRequest request, int? userId = null);
    Task<ServiceTicketDetailDto?> GetTicketByCodeAsync(string ticketCode, string? phoneVerification = null);
    Task<(bool Success, string Message)> SubmitQuoteDecisionAsync(int ticketId, int quoteId, string decision, string? notes = null, string? customerPhone = null);
    Task<List<ServiceTicketDetailDto>> GetAdminTicketsAsync(string? status = null, string? search = null, string? type = null);
    Task<ServiceTicketDetailDto?> GetTicketByIdAsync(int ticketId);
    Task<(bool Success, string Message)> UpdateTicketStatusByAdminAsync(
        int ticketId,
        string newStatus,
        string staffName,
        string? notes = null,
        string? physicalCondition = null,
        string? accessories = null,
        string? rejectReason = null,
        decimal? finalCost = null,
        int? warrantyMonthsAfterRepair = null);
    Task<(bool Success, string Message)> CreateQuoteAsync(int ticketId, string description, decimal amount, string staffName);
    Task<(bool Success, string Message)> AssignSerialsToOrderAsync(int orderId, List<OrderItemSerialsInput> items, string staffName);
    Task<List<SoldDeviceDto>> GetAllSoldDevicesAsync(string? search = null);
}
