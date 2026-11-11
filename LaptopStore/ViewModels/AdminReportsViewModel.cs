using System;
using System.Collections.Generic;

namespace LaptopStore.ViewModels;

public class AdminReportsViewModel
{
    
    public string Period { get; set; } = "month"; 
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public int TotalOrders { get; set; }
    public int NewOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalCompletedRevenue { get; set; }
    public decimal ActualCashCollected { get; set; }
    public decimal InstallmentCashCollected { get; set; }

    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
    public List<LowStockProductDto> LowStockProducts { get; set; } = new();

    public int TotalReceivedTickets { get; set; }
    public int HandedOverTickets { get; set; }
    public int AwaitingQuoteTickets { get; set; }
    public int ProcessingTickets { get; set; }
    public double AverageProcessingHours { get; set; }

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
