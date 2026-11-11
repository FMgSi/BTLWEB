namespace LaptopStore.ViewModels.Storefront;

public class CatalogViewModel
{
    public PagedResult<ProductApiDto> Products { get; set; } = new();

    public string? Search { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? PriceRange { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string SortBy { get; set; } = "newest";

    public List<string> AvailableBrands { get; set; } = new();
    public List<string> AvailableCategories { get; set; } = new();

    public int CurrentPage => Products.Page;
    public int PageSize => Products.PageSize;
    public int TotalItems => Products.TotalItems;
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 12));
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
