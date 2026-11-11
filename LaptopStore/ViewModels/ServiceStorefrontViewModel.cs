using System.Collections.Generic;

namespace LaptopStore.ViewModels;

public class ServiceStorefrontViewModel
{
    public string ActiveTab { get; set; } = "request";
    public List<ServiceCatalogDto> Catalog { get; set; } = new();
    public List<SoldDeviceDto> MyDevices { get; set; } = new();
    public ServiceTicketDetailDto? LookupResult { get; set; }
    public string? LookupCode { get; set; }
    public string? LookupPhone { get; set; }
    public string? LookupError { get; set; }
    public int? PreSelectedDeviceId { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
