using System;
using System.Collections.Generic;

namespace LaptopStore.ViewModels;

public class AdminReportsViewModel
{
    // 1. BỘ LỌC THỜI GIAN
    public string Period { get; set; } = "month"; // "today", "7days", "month", "custom"
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // 2. BÁO CÁO BÁN HÀNG
    public int TotalOrders { get; set; }
    public int NewOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalCompletedRevenue { get; set; }
    public decimal ActualCashCollected { get; set; }
    public decimal InstallmentCashCollected { get; set; }

    // 3. BÁO CÁO SẢN PHẨM
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();

    // 4. BÁO CÁO DỊCH VỤ SỬA CHỮA & BẢO HÀNH
    public int TotalReceivedTickets { get; set; }
    public int HandedOverTickets { get; set; }
    public int AwaitingQuoteTickets { get; set; }
    public int ProcessingTickets { get; set; }
    public double AverageProcessingHours { get; set; }

    // 5. DỮ LIỆU BIỂU ĐỒ DOANH THU
    public List<string> ChartLabels { get; set; } = new();
    public List<decimal> ChartValues { get; set; } = new();
}

public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantSpecs { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Configuration { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
}
