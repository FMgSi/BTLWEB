namespace LaptopStore.ViewModels;

public class ProductApiDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ScreenSize { get; set; } = string.Empty;
    public string Specs { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Badge { get; set; }
    public string BadgeColor { get; set; } = "bg-danger";
    public string Image { get; set; } = string.Empty;
}

public class ProductSpecDto
{
    public string CPU { get; set; } = string.Empty;
    public int RamGB { get; set; }
    public int StorageGB { get; set; }
    public string StorageType { get; set; } = string.Empty;
    public string GPU { get; set; } = string.Empty;
    public decimal ScreenSizeInch { get; set; }
    public int RefreshRateHz { get; set; }
    public decimal WeightKg { get; set; }
    public int BatteryWh { get; set; }
    public string OS { get; set; } = string.Empty;
}

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public int DiscountPercent { get; set; }
    public int StockQuantity { get; set; }
    public string Image { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public ProductSpecDto? Specification { get; set; }
}

public class PagedResult<T>
{
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}
