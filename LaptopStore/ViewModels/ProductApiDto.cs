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

public class PagedResult<T>
{
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<T> Items { get; set; } = new();
}
