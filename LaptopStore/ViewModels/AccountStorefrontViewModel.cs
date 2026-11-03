using System.Collections.Generic;

namespace LaptopStore.ViewModels;

public class AccountStorefrontViewModel
{
    public string ActiveTab { get; set; } = "orders";
    public bool IsAuthenticated { get; set; }
    public string CustomerName { get; set; } = "Khách hàng";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public string MemberTier { get; set; } = "Đồng";
    public string TierColor { get; set; } = "#a87552";
    public string NextTier { get; set; } = "Bạc";
    public decimal TotalSpent { get; set; }
    public decimal RemainingToNextTier { get; set; }
    public int ProgressPercent { get; set; }

    public List<OrderResponseDto> Orders { get; set; } = new();
    public List<SoldDeviceDto> Devices { get; set; } = new();

    public string? GuestOrderCode { get; set; }
    public string? GuestPhone { get; set; }
    public OrderResponseDto? GuestOrderResult { get; set; }
    public string? GuestLookupError { get; set; }
}
