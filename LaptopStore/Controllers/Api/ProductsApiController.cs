using LaptopStore.Data;
using LaptopStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers.Api;

[ApiController]
[Route("api/products")]
public class ProductsApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsApiController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? tag = null,
        [FromQuery] string? brands = null,
        [FromQuery] string? sizes = null,
        [FromQuery] string? cpus = null,
        [FromQuery] string? priceRange = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var query = _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .Where(p => p.IsActive)
            .AsQueryable();

        // 1. Lọc theo Tag danh mục linh hoạt
        if (!string.IsNullOrEmpty(tag) && tag != "Tất cả sản phẩm")
        {
            var cleanTag = tag.Trim().ToLower();
            if (cleanTag.Contains("gaming"))
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("gaming"));
            else if (cleanTag.Contains("văn phòng") || cleanTag.Contains("học tập"))
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("văn phòng") || p.Category!.CategoryName.ToLower().Contains("học tập"));
            else if (cleanTag.Contains("đồ họa") || cleanTag.Contains("kỹ thuật"))
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("đồ họa") || p.Category!.CategoryName.ToLower().Contains("kỹ thuật"));
            else if (cleanTag.Contains("mỏng nhẹ") || cleanTag.Contains("doanh nhân"))
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("mỏng nhẹ") || p.Category!.CategoryName.ToLower().Contains("doanh nhân"));
            else
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains(cleanTag) || cleanTag.Contains(p.Category!.CategoryName.ToLower()));
        }

        // 2. Lọc theo Brands (danh sách cách nhau bởi dấu phẩy)
        if (!string.IsNullOrEmpty(brands))
        {
            var brandList = brands.Split(',').Select(b => b.Trim().ToLower()).ToList();
            query = query.Where(p => brandList.Contains(p.Brand!.BrandName.ToLower()));
        }

        // 3. Lọc theo Kích thước màn hình (Hỗ trợ khoảng giá trị chính xác)
        if (!string.IsNullOrEmpty(sizes))
        {
            var sizeList = sizes.Split(',').Select(s => s.Replace("inch", "").Trim().ToLower()).ToList();
            var has13 = sizeList.Contains("13");
            var has14 = sizeList.Contains("14");
            var has15 = sizeList.Contains("15.6") || sizeList.Contains("15");
            var has16 = sizeList.Contains("16");
            var has17 = sizeList.Contains("17");

            query = query.Where(p => p.Specification != null && (
                (has13 && p.Specification.ScreenSizeInch >= 13.0m && p.Specification.ScreenSizeInch < 14.0m) ||
                (has14 && p.Specification.ScreenSizeInch >= 14.0m && p.Specification.ScreenSizeInch < 15.0m) ||
                (has15 && p.Specification.ScreenSizeInch >= 15.0m && p.Specification.ScreenSizeInch < 16.0m) ||
                (has16 && p.Specification.ScreenSizeInch >= 16.0m && p.Specification.ScreenSizeInch < 17.0m) ||
                (has17 && p.Specification.ScreenSizeInch >= 17.0m)
            ));
        }

        // 4. Lọc theo Vi xử lý (CPU)
        if (!string.IsNullOrEmpty(cpus))
        {
            var cpuList = cpus.Split(',').Select(c => c.Trim().ToLower()).ToList();
            var hasI5 = cpuList.Any(c => c.Contains("i5"));
            var hasI7 = cpuList.Any(c => c.Contains("i7"));
            var hasI9 = cpuList.Any(c => c.Contains("i9"));
            var hasUltra = cpuList.Any(c => c.Contains("ultra"));
            var hasR5 = cpuList.Any(c => c.Contains("ryzen 5"));
            var hasR7 = cpuList.Any(c => c.Contains("ryzen 7"));
            var hasR9 = cpuList.Any(c => c.Contains("ryzen 9"));
            var hasApple = cpuList.Any(c => c.Contains("apple"));

            query = query.Where(p => p.Specification != null && p.Specification.CPU != null && (
                (hasI5 && (p.Specification.CPU.Contains("i5") || p.Specification.CPU.Contains("Core 5"))) ||
                (hasI7 && p.Specification.CPU.Contains("i7")) ||
                (hasI9 && p.Specification.CPU.Contains("i9")) ||
                (hasUltra && p.Specification.CPU.Contains("Ultra")) ||
                (hasR5 && p.Specification.CPU.Contains("Ryzen 5")) ||
                (hasR7 && p.Specification.CPU.Contains("Ryzen 7")) ||
                (hasR9 && p.Specification.CPU.Contains("Ryzen 9")) ||
                (hasApple && (p.Specification.CPU.Contains("Apple") || p.Specification.CPU.Contains("M1") || p.Specification.CPU.Contains("M2") || p.Specification.CPU.Contains("M3")))
            ));
        }

        // 4. Lọc theo khoảng giá
        if (priceRange == "under10")
            query = query.Where(p => (p.DiscountPrice ?? p.Price) < 10000000);
        else if (priceRange == "10to20")
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= 10000000 && (p.DiscountPrice ?? p.Price) <= 20000000);
        else if (priceRange == "20to30")
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= 20000000 && (p.DiscountPrice ?? p.Price) <= 30000000);
        else if (priceRange == "above30")
            query = query.Where(p => (p.DiscountPrice ?? p.Price) > 30000000);
        else if (priceRange == "custom")
        {
            if (minPrice.HasValue) query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
        }

        // 5. Sắp xếp (Sort)
        query = sortBy switch
        {
            "price-asc" => query.OrderBy(p => (p.DiscountPrice ?? p.Price)),
            "price-desc" => query.OrderByDescending(p => (p.DiscountPrice ?? p.Price)),
            _ => query.OrderByDescending(p => p.ProductId)
        };

        var total = await query.CountAsync();
        var rawList = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        // Chuẩn hóa format DTO cho React
        var result = rawList.Select(p =>
        {
            var curPrice = p.DiscountPrice ?? p.Price;
            decimal? oldPrice = p.DiscountPrice.HasValue ? p.Price : null;
            string? badge = null;
            string badgeColor = "bg-danger";

            if (p.DiscountPrice.HasValue && p.Price > p.DiscountPrice.Value)
            {
                var discountPercent = Math.Round((1 - (p.DiscountPrice.Value / p.Price)) * 100);
                badge = $"-{discountPercent}%";
            }
            else
            {
                badge = "MỚI";
                badgeColor = "bg-success";
            }

            var spec = p.Specification;
            var specStr = spec != null
                ? $"{spec.CPU} / {spec.RamGB}GB / {spec.StorageGB}GB / {spec.ScreenSizeInch}\" / {spec.OS}"
                : "";

            return new ProductApiDto
            {
                Id = p.ProductId,
                Name = p.ProductName,
                Brand = p.Brand?.BrandName ?? "",
                Category = p.Category?.CategoryName ?? "",
                ScreenSize = spec != null ? $"{spec.ScreenSizeInch} inch" : "",
                Specs = specStr,
                Price = curPrice,
                OriginalPrice = oldPrice,
                Badge = badge,
                BadgeColor = badgeColor,
                Image = p.ThumbnailUrl ?? "/images/default.png"
            };
        }).ToList();

        return Ok(new PagedResult<ProductApiDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = result
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Include(x => x.Specification)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.ProductId == id);

        if (p == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });
        return Ok(p);
    }
}