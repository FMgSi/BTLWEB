namespace LaptopStore.ViewModels;

public class ProductFilterViewModel
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
}
