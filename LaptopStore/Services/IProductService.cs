using LaptopStore.Models;
using LaptopStore.ViewModels;

namespace LaptopStore.Services;

public interface IProductService
{
    Task<PagedResult<ProductApiDto>> GetProductsApiAsync(
        string? q = null,
        string? tag = null,
        string? brands = null,
        string? sizes = null,
        string? cpus = null,
        string? rams = null,
        string? priceRange = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = "newest",
        int page = 1,
        int pageSize = 12);

    Task<ProductDetailDto?> GetProductDetailApiAsync(int id);

    Task<(List<Product> Items, int TotalCount, List<Brand> Brands, List<Category> Categories)> GetAdminProductsAsync(
        string? keyword,
        int? brandId,
        int? categoryId,
        string? status,
        string? priceRange,
        int page,
        int pageSize);

    Task<Product?> GetProductByIdAsync(int id);

    Task<bool> CreateProductAsync(ProductEditViewModel vm);

    Task<bool> UpdateProductAsync(ProductEditViewModel vm);

    Task<bool> DeleteProductAsync(int id);
}
