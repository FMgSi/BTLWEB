using LaptopStore.Models;

namespace LaptopStore.ViewModels;

public class AdminDashboardViewModel
{
    public decimal MonthlyRevenue { get; set; } = 1450000000m;
    public double RevenueGrowth { get; set; } = 12.0;
    public int NewOrdersCount { get; set; } = 342;
    public int LaptopsSoldCount { get; set; } = 189;
    public double SalesGrowth { get; set; } = 18.0;
    public int LowStockCount { get; set; } = 15;

    public List<RecentOrderDto> RecentOrders { get; set; } = new();
    public List<TopProductDto> TopSellingProducts { get; set; } = new();
    public List<RevenuePointDto> RevenueTrends { get; set; } = new();
    public List<CategoryShareDto> CategoryShares { get; set; } = new();
}

public class RecentOrderDto
{
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty; // "Đã thanh toán", "Đang xử lý", "Đã xác nhận", "Hoàn thành"
    public string StatusBadgeClass { get; set; } = string.Empty;
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int SoldCount { get; set; }
}

public class RevenuePointDto
{
    public string Day { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class CategoryShareDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class CustomerDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public string MembershipTier { get; set; } = string.Empty; // "Kim cương", "Vàng", "Bạc", "Đồng"
    public string TierBadgeClass { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class DiscountDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty; // "Giảm theo %", "Giảm số tiền"
    public string DiscountValueText { get; set; } = string.Empty;
    public string ConditionText { get; set; } = string.Empty;
    public string UsageText { get; set; } = string.Empty;
    public string ExpiryText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Đang hoạt động", "Hết hạn", "Chưa bắt đầu"
    public string StatusBadgeClass { get; set; } = string.Empty;
}

public class SystemSettingsViewModel
{
    // Cửa hàng
    public string StoreName { get; set; } = "LAPTOP WORLD VIỆT NAM";
    public string Hotline { get; set; } = "1900 8888";
    public string SupportEmail { get; set; } = "hotro@laptopworld.vn";
    public string StoreAddress { get; set; } = "Số 123 Đường Cầu Giấy, Quận Cầu Giấy, TP. Hà Nội";
    public string TaxId { get; set; } = "0109887766";

    // COD
    public bool EnableCod { get; set; } = true;

    // VNPAY
    public bool EnableVnPay { get; set; } = true;
    public string VnPayTmnCode { get; set; } = "LAPTOPW1";
    public string VnPayHashKey { get; set; } = "RAOCTARVBMNCKHYZXWUQJDEFKSOQPZMA";
    public string VnPayUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    // VietQR
    public string VietQrBankName { get; set; } = "MB Bank (Ngân hàng Quân Đội)";
    public string VietQrAccountNumber { get; set; } = "098877665544";
    public string VietQrAccountHolder { get; set; } = "NGUYEN VAN A";

    // MoMo
    public bool EnableMoMo { get; set; } = false;
    public string MoMoPartnerCode { get; set; } = "MOMOBKUN20180529";
    public string MoMoAccessKey { get; set; } = "klm05TvNBzhg7h7j";
    public string MoMoSecretKey { get; set; } = "at67qH6mk8w5Y1nAyMoTvMWACxb8iYr8";

    // ZaloPay
    public bool EnableZaloPay { get; set; } = false;
    public string ZaloPayAppId { get; set; } = "2554";
    public string ZaloPayKey1 { get; set; } = "sdngKKJv5gah2838vhf83";
}
