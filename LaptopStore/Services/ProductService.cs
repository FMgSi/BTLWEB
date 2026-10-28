using LaptopStore.Data;
using LaptopStore.Models;
using LaptopStore.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LaptopStore.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<PagedResult<ProductApiDto>> GetProductsApiAsync(
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
        int pageSize = 12)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .Where(p => p.IsActive)
            .AsQueryable();

        // 1. Tìm kiếm theo từ khóa
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(kw) ||
                (p.Brand != null && p.Brand.BrandName.ToLower().Contains(kw)) ||
                (p.Category != null && p.Category.CategoryName.ToLower().Contains(kw)) ||
                (p.Specification != null && p.Specification.CPU != null && p.Specification.CPU.ToLower().Contains(kw)) ||
                (p.Specification != null && p.Specification.GPU != null && p.Specification.GPU.ToLower().Contains(kw))
            );
        }

        // 2. Lọc theo Tag / Danh mục
        if (!string.IsNullOrEmpty(tag) && tag != "Tất cả sản phẩm")
        {
            var cleanTag = tag.Trim().ToLower();
            if (cleanTag.Contains("sinh viên"))
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= 30000000);
            }
            else if (cleanTag.Contains("gaming"))
            {
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("gaming"));
            }
            else if (cleanTag.Contains("văn phòng") || cleanTag.Contains("học tập"))
            {
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("văn phòng") || p.Category!.CategoryName.ToLower().Contains("học tập"));
            }
            else if (cleanTag.Contains("đồ họa") || cleanTag.Contains("kỹ thuật"))
            {
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("đồ họa") || p.Category!.CategoryName.ToLower().Contains("kỹ thuật"));
            }
            else if (cleanTag.Contains("macbook") || cleanTag.Contains("apple"))
            {
                query = query.Where(p => (p.Brand != null && p.Brand.BrandName.ToLower().Contains("apple")) || p.ProductName.ToLower().Contains("macbook") || p.Category!.CategoryName.ToLower().Contains("macbook"));
            }
            else if (cleanTag.Contains("mỏng nhẹ") || cleanTag.Contains("doanh nhân"))
            {
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains("mỏng nhẹ") || p.Category!.CategoryName.ToLower().Contains("doanh nhân"));
            }
            else
            {
                query = query.Where(p => p.Category!.CategoryName.ToLower().Contains(cleanTag) || cleanTag.Contains(p.Category!.CategoryName.ToLower()));
            }
        }

        // 3. Lọc theo Thương hiệu
        if (!string.IsNullOrEmpty(brands))
        {
            var brandList = brands.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(b => b.Trim().ToLower())
                                  .ToList();
            if (brandList.Any())
            {
                query = query.Where(p => p.Brand != null && brandList.Contains(p.Brand.BrandName.ToLower()));
            }
        }

        // 4. Lọc theo Màn hình
        if (!string.IsNullOrEmpty(sizes))
        {
            var sizeList = sizes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Replace("inch", "").Trim())
                                .ToList();
            if (sizeList.Any())
            {
                query = query.Where(p => p.Specification != null &&
                    sizeList.Any(s => p.Specification.ScreenSizeInch.ToString().StartsWith(s)));
            }
        }

        // 5. Lọc theo CPU
        if (!string.IsNullOrEmpty(cpus))
        {
            var cpuList = cpus.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(c => c.Trim().ToLower())
                              .ToList();
            if (cpuList.Any())
            {
                query = query.Where(p => p.Specification != null && p.Specification.CPU != null &&
                    cpuList.Any(c => p.Specification.CPU.ToLower().Contains(c)));
            }
        }

        // 6. Lọc theo RAM
        if (!string.IsNullOrEmpty(rams))
        {
            var ramValues = rams.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(r => Regex.Match(r, @"\d+").Value)
                                .Where(r => int.TryParse(r, out _))
                                .Select(int.Parse)
                                .ToList();
            if (ramValues.Any())
            {
                query = query.Where(p => p.Specification != null && ramValues.Contains(p.Specification.RamGB));
            }
        }

        // 7. Lọc theo Giá
        if (!string.IsNullOrEmpty(priceRange) && priceRange != "all")
        {
            switch (priceRange)
            {
                case "under10":
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) < 10000000);
                    break;
                case "10to20":
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= 10000000 && (p.DiscountPrice ?? p.Price) <= 20000000);
                    break;
                case "20to30":
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= 20000000 && (p.DiscountPrice ?? p.Price) <= 30000000);
                    break;
                case "under30":
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) <= 30000000);
                    break;
                case "above30":
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) > 30000000);
                    break;
            }
        }
        else if (minPrice.HasValue || maxPrice.HasValue)
        {
            if (minPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
        }

        // 8. Sắp xếp
        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "name_asc" => query.OrderBy(p => p.ProductName),
            "name_desc" => query.OrderByDescending(p => p.ProductName),
            _ => query.OrderByDescending(p => p.ProductId)
        };

        var total = await query.CountAsync();
        var rawList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = rawList.Select(p =>
        {
            var curPrice = p.DiscountPrice ?? p.Price;
            decimal? oldPrice = p.DiscountPrice.HasValue ? p.Price : null;
            var spec = p.Specification;

            var specParts = new List<string>();
            if (spec != null)
            {
                if (!string.IsNullOrEmpty(spec.CPU)) specParts.Add(spec.CPU);
                if (spec.RamGB > 0) specParts.Add($"{spec.RamGB}GB");
                if (spec.StorageGB > 0)
                {
                    var st = spec.StorageGB >= 1024 ? $"{spec.StorageGB / 1024}TB" : $"{spec.StorageGB}GB";
                    specParts.Add($"{st} {spec.StorageType ?? "SSD"}".Trim());
                }
                if (!string.IsNullOrEmpty(spec.GPU)) specParts.Add(spec.GPU);
            }
            var specStr = specParts.Any() ? string.Join(" • ", specParts) : "Cấu hình tiêu chuẩn";

            string? badge = null;
            string badgeColor = "bg-danger";
            if (p.DiscountPrice.HasValue && p.Price > p.DiscountPrice.Value)
            {
                int percent = (int)Math.Round((1 - (p.DiscountPrice.Value / p.Price)) * 100);
                badge = $"Giảm {percent}%";
                badgeColor = "bg-danger";
            }
            else if (p.StockQuantity <= 0)
            {
                badge = "Hết hàng";
                badgeColor = "bg-secondary";
            }
            else if (p.StockQuantity <= 5)
            {
                badge = "Sắp hết hàng";
                badgeColor = "bg-warning text-dark";
            }

            return new ProductApiDto
            {
                Id = p.ProductId,
                Name = p.ProductName,
                Brand = p.Brand?.BrandName ?? "",
                Category = p.Category?.CategoryName ?? "",
                ScreenSize = spec != null && spec.ScreenSizeInch > 0 ? $"{spec.ScreenSizeInch} inch" : "",
                Specs = specStr,
                Price = curPrice,
                OriginalPrice = oldPrice,
                Badge = badge,
                BadgeColor = badgeColor,
                Image = p.ThumbnailUrl ?? "/images/default.png"
            };
        }).ToList();

        return new PagedResult<ProductApiDto>
        {
            TotalItems = total,
            Page = page,
            PageSize = pageSize,
            Items = result
        };
    }

    public async Task<ProductDetailDto?> GetProductDetailApiAsync(int id)
    {
        var p = await _db.Products
            .AsNoTracking()
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .Include(x => x.Specification)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.ProductId == id && x.IsActive);

        if (p == null) return null;

        var curPrice = p.DiscountPrice ?? p.Price;
        decimal? oldPrice = p.DiscountPrice.HasValue ? p.Price : null;
        int discountPercent = 0;
        if (p.DiscountPrice.HasValue && p.Price > p.DiscountPrice.Value)
        {
            discountPercent = (int)Math.Round((1 - (p.DiscountPrice.Value / p.Price)) * 100);
        }

        var imageList = p.Images.OrderBy(img => img.DisplayOrder).Select(img => img.ImageUrl).ToList();
        if (!string.IsNullOrEmpty(p.ThumbnailUrl) && !imageList.Contains(p.ThumbnailUrl))
        {
            imageList.Insert(0, p.ThumbnailUrl);
        }

        return new ProductDetailDto
        {
            Id = p.ProductId,
            Name = p.ProductName,
            Brand = p.Brand?.BrandName ?? "",
            Category = p.Category?.CategoryName ?? "",
            Price = curPrice,
            OriginalPrice = oldPrice,
            DiscountPercent = discountPercent,
            StockQuantity = p.StockQuantity,
            Image = p.ThumbnailUrl ?? "/images/default.png",
            Images = imageList,
            Description = p.Description ?? "Sản phẩm chính hãng với bảo hành 12-24 tháng.",
            Specification = p.Specification == null ? null : new ProductSpecDto
            {
                CPU = p.Specification.CPU ?? "",
                RamGB = p.Specification.RamGB,
                StorageGB = p.Specification.StorageGB,
                StorageType = p.Specification.StorageType ?? "SSD NVMe",
                GPU = p.Specification.GPU ?? "",
                ScreenSizeInch = p.Specification.ScreenSizeInch,
                RefreshRateHz = p.Specification.RefreshRateHz,
                WeightKg = p.Specification.WeightKg,
                BatteryWh = p.Specification.BatteryWh,
                OS = p.Specification.OS ?? "Windows 11"
            }
        };
    }

    public async Task<(List<Product> Items, int TotalCount, List<Brand> Brands, List<Category> Categories)> GetAdminProductsAsync(
        string? keyword,
        int? brandId,
        int? categoryId,
        string? status,
        string? priceRange,
        int page,
        int pageSize)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(kw) ||
                (p.Brand != null && p.Brand.BrandName.ToLower().Contains(kw)) ||
                (p.Category != null && p.Category.CategoryName.ToLower().Contains(kw))
            );
        }

        if (brandId.HasValue && brandId.Value > 0)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            switch (status)
            {
                case "instock":
                    query = query.Where(p => p.StockQuantity > 5);
                    break;
                case "lowstock":
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 5);
                    break;
                case "outofstock":
                    query = query.Where(p => p.StockQuantity <= 0);
                    break;
                case "inactive":
                    query = query.Where(p => !p.IsActive);
                    break;
            }
        }

        if (!string.IsNullOrEmpty(priceRange) && priceRange != "all")
        {
            switch (priceRange)
            {
                case "under15":
                    query = query.Where(p => p.Price < 15000000);
                    break;
                case "15to25":
                    query = query.Where(p => p.Price >= 15000000 && p.Price <= 25000000);
                    break;
                case "above25":
                    query = query.Where(p => p.Price > 25000000);
                    break;
            }
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderByDescending(p => p.ProductId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var brands = await _db.Brands.AsNoTracking().ToListAsync();
        var categories = await _db.Categories.AsNoTracking().ToListAsync();

        return (list, total, brands, categories);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specification)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id);
    }

    public async Task<bool> CreateProductAsync(ProductEditViewModel vm)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            string? imgPath = null;
            if (vm.UploadedImage != null && vm.UploadedImage.Length > 0)
            {
                imgPath = await SaveImageFileAsync(vm.UploadedImage);
            }

            var product = new Product
            {
                ProductName = vm.ProductName.Trim(),
                BrandId = vm.BrandId,
                CategoryId = vm.CategoryId,
                Price = vm.Price,
                DiscountPrice = vm.DiscountPrice,
                StockQuantity = vm.StockQuantity,
                Description = vm.Description,
                ThumbnailUrl = imgPath ?? "/images/default.png",
                IsActive = true
            };

            // Phân tích RAM và Storage
            var (ramGb, storageGb, storageType) = ParseSpecs(vm.RAM, vm.Storage);

            product.Specification = new ProductSpecification
            {
                CPU = vm.CPU,
                RamGB = ramGb,
                StorageGB = storageGb,
                StorageType = storageType,
                GPU = vm.GPU,
                ScreenSizeInch = vm.ScreenSizeInch ?? 15.6m,
                RefreshRateHz = vm.RefreshRateHz ?? 144,
                WeightKg = vm.WeightKg ?? 1.9m,
                BatteryWh = vm.BatteryWh ?? 60,
                OS = !string.IsNullOrWhiteSpace(vm.OS) ? vm.OS : "Windows 11 Home"
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> UpdateProductAsync(ProductEditViewModel vm)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var p = await _db.Products
                .Include(p => p.Specification)
                .FirstOrDefaultAsync(p => p.ProductId == vm.ProductId);

            if (p == null) return false;

            if (vm.UploadedImage != null && vm.UploadedImage.Length > 0)
            {
                var newImg = await SaveImageFileAsync(vm.UploadedImage);
                p.ThumbnailUrl = newImg;
            }

            p.ProductName = vm.ProductName.Trim();
            p.BrandId = vm.BrandId;
            p.CategoryId = vm.CategoryId;
            p.Price = vm.Price;
            p.DiscountPrice = vm.DiscountPrice;
            p.StockQuantity = vm.StockQuantity;
            p.Description = vm.Description;

            var (ramGb, storageGb, storageType) = ParseSpecs(vm.RAM, vm.Storage);

            if (p.Specification == null)
            {
                p.Specification = new ProductSpecification
                {
                    ProductId = p.ProductId,
                    CPU = vm.CPU,
                    RamGB = ramGb,
                    StorageGB = storageGb,
                    StorageType = storageType,
                    GPU = vm.GPU,
                    ScreenSizeInch = vm.ScreenSizeInch ?? 15.6m,
                    RefreshRateHz = vm.RefreshRateHz ?? 144,
                    WeightKg = vm.WeightKg ?? 1.9m,
                    BatteryWh = vm.BatteryWh ?? 60,
                    OS = !string.IsNullOrWhiteSpace(vm.OS) ? vm.OS : "Windows 11 Home"
                };
                _db.ProductSpecifications.Add(p.Specification);
            }
            else
            {
                p.Specification.CPU = vm.CPU;
                p.Specification.RamGB = ramGb;
                p.Specification.StorageGB = storageGb;
                p.Specification.StorageType = storageType;
                p.Specification.GPU = vm.GPU;

                if (vm.ScreenSizeInch.HasValue) p.Specification.ScreenSizeInch = vm.ScreenSizeInch.Value;
                if (vm.RefreshRateHz.HasValue) p.Specification.RefreshRateHz = vm.RefreshRateHz.Value;
                if (vm.WeightKg.HasValue) p.Specification.WeightKg = vm.WeightKg.Value;
                if (vm.BatteryWh.HasValue) p.Specification.BatteryWh = vm.BatteryWh.Value;
                if (!string.IsNullOrWhiteSpace(vm.OS)) p.Specification.OS = vm.OS;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;

        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<string> SaveImageFileAsync(IFormFile file)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var safeFileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, safeFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{safeFileName}";
    }

    private static (int RamGb, int StorageGb, string StorageType) ParseSpecs(string? ramStr, string? storageStr)
    {
        int ramGb = 16;
        int storageGb = 512;
        string storageType = "SSD NVMe";

        if (!string.IsNullOrEmpty(ramStr))
        {
            var match = Regex.Match(ramStr, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int parsedRam))
            {
                ramGb = parsedRam;
            }
        }

        if (!string.IsNullOrEmpty(storageStr))
        {
            var match = Regex.Match(storageStr, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int parsedStorage))
            {
                storageGb = parsedStorage;
                if (storageGb <= 4) storageGb *= 1024; // 1TB, 2TB, 4TB
            }

            if (storageStr.Contains("HDD", StringComparison.OrdinalIgnoreCase))
            {
                storageType = "HDD";
            }
            else if (storageStr.Contains("PCIe 4.0", StringComparison.OrdinalIgnoreCase))
            {
                storageType = "SSD PCIe 4.0";
            }
            else
            {
                storageType = "SSD";
            }
        }

        return (ramGb, storageGb, storageType);
    }
}
