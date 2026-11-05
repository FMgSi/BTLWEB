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
            .Include(p => p.Images)
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
                Image = (!string.IsNullOrEmpty(p.ThumbnailUrl) && p.ThumbnailUrl != "/images/default.png")
                    ? p.ThumbnailUrl
                    : (p.Images?.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault() ?? p.ThumbnailUrl ?? "/images/default.png")
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
            .Include(x => x.Variants)
                .ThenInclude(v => v.Color)
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

        // Gom nhóm ảnh theo từng màu sắc (DisplayOrder từ 1 đến 4)
        var colorImagesMap = new Dictionary<int, List<string>>();
        foreach (var grp in p.Images.Where(img => img.ColorId.HasValue).GroupBy(img => img.ColorId!.Value))
        {
            var imgs = grp.OrderBy(img => img.DisplayOrder).Select(img => img.ImageUrl).ToList();
            if (imgs.Any())
            {
                colorImagesMap[grp.Key] = imgs;
            }
        }

        var variantsDto = p.Variants
            .Where(v => v.IsActive)
            .OrderBy(v => v.Color?.Name)
            .ThenBy(v => v.RamGB)
            .ThenBy(v => v.StorageGB)
            .Select(v =>
            {
                List<string> varImgs;
                if (colorImagesMap.TryGetValue(v.ColorId, out var mapped) && mapped.Any())
                {
                    varImgs = mapped;
                }
                else if (!string.IsNullOrEmpty(v.ImageUrl))
                {
                    varImgs = new List<string> { v.ImageUrl };
                }
                else
                {
                    varImgs = imageList;
                }

                return new ProductVariantDto
                {
                    VariantId = v.VariantId,
                    ProductId = v.ProductId,
                    ProductName = p.ProductName,
                    ColorId = v.ColorId,
                    ColorName = v.Color?.Name ?? "Mặc định",
                    HexCode = v.Color?.HexCode ?? "#CBD5E1",
                    RamGB = v.RamGB,
                    StorageGB = v.StorageGB,
                    SKU = v.SKU,
                    StockQuantity = v.StockQuantity,
                    Price = v.Price,
                    DiscountPrice = v.DiscountPrice,
                    EffectivePrice = v.EffectivePrice,
                    ImageUrl = varImgs.FirstOrDefault() ?? v.ImageUrl ?? p.ThumbnailUrl,
                    Images = varImgs,
                    IsActive = v.IsActive
                };
            }).ToList();

        int totalStock = variantsDto.Any() ? variantsDto.Sum(v => v.StockQuantity) : p.StockQuantity;

        return new ProductDetailDto
        {
            Id = p.ProductId,
            Name = p.ProductName,
            Brand = p.Brand?.BrandName ?? "",
            Category = p.Category?.CategoryName ?? "",
            Price = curPrice,
            OriginalPrice = oldPrice,
            DiscountPercent = discountPercent,
            StockQuantity = totalStock,
            Image = p.ThumbnailUrl ?? "/images/default.png",
            Images = imageList,
            ColorImages = colorImagesMap,
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
            },
            Variants = variantsDto
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
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                string? imgPath = null;
                var mainImgFile = vm.Slot1Image ?? vm.UploadedImage;
                if (mainImgFile != null && mainImgFile.Length > 0)
                {
                    imgPath = await SaveImageFileAsync(mainImgFile);
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

                // Phân tích RAM và Storage (ưu tiên dropdown chuẩn RamGB/StorageGB, tương thích ngược với chuỗi)
                int ramGb = vm.RamGB ?? 16;
                int storageGb = vm.StorageGB ?? 512;
                string storageType = !string.IsNullOrWhiteSpace(vm.StorageType) ? vm.StorageType : "SSD NVMe";

                if (!vm.RamGB.HasValue || !vm.StorageGB.HasValue || string.IsNullOrWhiteSpace(vm.StorageType))
                {
                    var parsed = ParseSpecs(vm.RAM, vm.Storage);
                    if (!vm.RamGB.HasValue) ramGb = parsed.RamGb;
                    if (!vm.StorageGB.HasValue) storageGb = parsed.StorageGb;
                    if (string.IsNullOrWhiteSpace(vm.StorageType)) storageType = parsed.StorageType;
                }

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

                // Xác định màu sắc khởi tạo
                int? colorId = vm.SelectedColorId;
                if (!colorId.HasValue || colorId.Value <= 0)
                {
                    var defaultColor = await _db.Colors.FirstOrDefaultAsync(c => c.IsActive);
                    if (defaultColor != null) colorId = defaultColor.ColorId;
                }

                // Lưu các ô ảnh 1, 2, 3, 4 nếu có
                async Task SaveSlotAsync(int slot, IFormFile? file, string? preSavedUrl = null)
                {
                    string? url = preSavedUrl;
                    if (file != null && file.Length > 0)
                    {
                        url = await SaveImageFileAsync(file);
                    }
                    if (!string.IsNullOrEmpty(url))
                    {
                        _db.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.ProductId,
                            ColorId = colorId,
                            DisplayOrder = slot,
                            ImageUrl = url
                        });
                    }
                }

                await SaveSlotAsync(1, vm.Slot1Image, imgPath);
                await SaveSlotAsync(2, vm.Slot2Image);
                await SaveSlotAsync(3, vm.Slot3Image);
                await SaveSlotAsync(4, vm.Slot4Image);

                // Tạo biến thể ban đầu tương ứng với màu đã chọn
                if (colorId.HasValue && colorId.Value > 0)
                {
                    var initialVariant = new ProductVariant
                    {
                        ProductId = product.ProductId,
                        ColorId = colorId.Value,
                        RamGB = ramGb,
                        StorageGB = storageGb,
                        Price = vm.Price,
                        DiscountPrice = vm.DiscountPrice,
                        StockQuantity = vm.StockQuantity,
                        SKU = $"SKU-{product.ProductId}-{colorId.Value}-{ramGb}-{storageGb}",
                        ImageUrl = imgPath ?? product.ThumbnailUrl,
                        IsActive = true
                    };
                    _db.ProductVariants.Add(initialVariant);
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
        });
    }

    public async Task<bool> UpdateProductAsync(ProductEditViewModel vm)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var p = await _db.Products
                    .Include(p => p.Specification)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.ProductId == vm.ProductId);

                if (p == null) return false;

                if (vm.UploadedImage != null && vm.UploadedImage.Length > 0)
                {
                    var newImg = await SaveImageFileAsync(vm.UploadedImage);
                    p.ThumbnailUrl = newImg;
                }

                // Xử lý 4 ô ảnh độc lập cho màu sắc đang chọn (vm.SelectedColorId)
                if (vm.SelectedColorId.HasValue && vm.SelectedColorId.Value > 0)
                {
                    int colorId = vm.SelectedColorId.Value;
                    bool colorExists = await _db.Colors.AnyAsync(c => c.ColorId == colorId);
                    if (colorExists)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                        async Task HandleColorSlotAsync(int slot, IFormFile? file, bool delete)
                        {
                            if (slot < 1 || slot > 4) return;

                            var existing = p.Images.FirstOrDefault(img => img.ColorId == colorId && img.DisplayOrder == slot);

                            if (file != null && file.Length > 0)
                            {
                                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                                if (allowedExtensions.Contains(ext) && file.Length <= 5 * 1024 * 1024)
                                {
                                    string newUrl = await SaveImageFileAsync(file);
                                    if (existing != null)
                                    {
                                        existing.ImageUrl = newUrl;
                                    }
                                    else
                                    {
                                        var newImg = new ProductImage
                                        {
                                            ProductId = p.ProductId,
                                            ColorId = colorId,
                                            DisplayOrder = slot,
                                            ImageUrl = newUrl
                                        };
                                        _db.ProductImages.Add(newImg);
                                        p.Images.Add(newImg);
                                    }

                                    // Nếu là ô 1 (Ảnh đại diện), đồng bộ với các variant cùng màu và luôn cập nhật ảnh đại diện của laptop
                                    if (slot == 1)
                                    {
                                        foreach (var v in p.Variants.Where(v => v.ColorId == colorId))
                                        {
                                            v.ImageUrl = newUrl;
                                        }

                                        p.ThumbnailUrl = newUrl;
                                    }
                                }
                            }
                            else if (delete && slot > 1 && existing != null)
                            {
                                _db.ProductImages.Remove(existing);
                                p.Images.Remove(existing);
                            }
                        }

                        await HandleColorSlotAsync(1, vm.Slot1Image, false);
                        await HandleColorSlotAsync(2, vm.Slot2Image, vm.DeleteSlot2);
                        await HandleColorSlotAsync(3, vm.Slot3Image, vm.DeleteSlot3);
                        await HandleColorSlotAsync(4, vm.Slot4Image, vm.DeleteSlot4);
                    }
                }

                p.ProductName = vm.ProductName.Trim();
                p.BrandId = vm.BrandId;
                p.CategoryId = vm.CategoryId;
                p.Price = vm.Price;
                p.DiscountPrice = vm.DiscountPrice;
                p.StockQuantity = vm.StockQuantity;
                p.Description = vm.Description;

                // Phân tích RAM và Storage (ưu tiên dropdown chuẩn RamGB/StorageGB, tương thích ngược với chuỗi)
                int ramGb = vm.RamGB ?? 16;
                int storageGb = vm.StorageGB ?? 512;
                string storageType = !string.IsNullOrWhiteSpace(vm.StorageType) ? vm.StorageType : "SSD NVMe";

                if (!vm.RamGB.HasValue || !vm.StorageGB.HasValue || string.IsNullOrWhiteSpace(vm.StorageType))
                {
                    var parsed = ParseSpecs(vm.RAM, vm.Storage);
                    if (!vm.RamGB.HasValue) ramGb = parsed.RamGb;
                    if (!vm.StorageGB.HasValue) storageGb = parsed.StorageGb;
                    if (string.IsNullOrWhiteSpace(vm.StorageType)) storageType = parsed.StorageType;
                }

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
        });
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;

        // Kiểm tra ràng buộc toàn vẹn: Không xóa sản phẩm đã phát sinh chi tiết đơn hàng
        var hasOrders = await _db.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
        {
            return false;
        }

        try
        {
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task<List<ProductColorImagesViewModel>> GetProductColorImagesAsync(int productId)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants)
                .ThenInclude(v => v.Color)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null) return new List<ProductColorImagesViewModel>();

        // Lấy danh sách màu sắc thuộc sản phẩm (từ variants và images)
        var colorMap = new Dictionary<int, Color>();
        foreach (var v in product.Variants)
        {
            if (v.Color != null && !colorMap.ContainsKey(v.ColorId))
            {
                colorMap[v.ColorId] = v.Color;
            }
        }

        var imageColorIds = product.Images.Where(i => i.ColorId.HasValue).Select(i => i.ColorId!.Value).Distinct();
        foreach (var cid in imageColorIds)
        {
            if (!colorMap.ContainsKey(cid))
            {
                var c = await _db.Colors.FindAsync(cid);
                if (c != null) colorMap[cid] = c;
            }
        }

        // Nếu sản phẩm chưa có biến thể nào thì lấy toàn bộ màu active để nhân viên có thể tải trước ảnh
        if (!colorMap.Any())
        {
            var activeColors = await _db.Colors.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            foreach (var c in activeColors)
            {
                colorMap[c.ColorId] = c;
            }
        }

        var result = new List<ProductColorImagesViewModel>();
        foreach (var kvp in colorMap.OrderBy(x => x.Value.Name))
        {
            var color = kvp.Value;
            var imgs = product.Images.Where(i => i.ColorId == color.ColorId).ToList();

            var slot1 = imgs.FirstOrDefault(i => i.DisplayOrder == 1)?.ImageUrl;
            if (string.IsNullOrEmpty(slot1))
            {
                slot1 = product.Variants.FirstOrDefault(v => v.ColorId == color.ColorId && !string.IsNullOrEmpty(v.ImageUrl))?.ImageUrl;
            }
            if (string.IsNullOrEmpty(slot1) && colorMap.Any() && colorMap.First().Key == color.ColorId)
            {
                slot1 = product.ThumbnailUrl;
            }

            var slot2 = imgs.FirstOrDefault(i => i.DisplayOrder == 2)?.ImageUrl;
            var slot3 = imgs.FirstOrDefault(i => i.DisplayOrder == 3)?.ImageUrl;
            var slot4 = imgs.FirstOrDefault(i => i.DisplayOrder == 4)?.ImageUrl;

            result.Add(new ProductColorImagesViewModel
            {
                ColorId = color.ColorId,
                ColorName = color.Name,
                HexCode = color.HexCode ?? "#CBD5E1",
                Slot1Url = slot1,
                Slot2Url = slot2,
                Slot3Url = slot3,
                Slot4Url = slot4
            });
        }

        return result;
    }

    public async Task<List<ModelSuggestionDto>> SuggestModelsAsync(string? query)
    {
        var q = _db.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            q = q.Where(p => p.ProductName.ToLower().Contains(term) ||
                             (p.Brand != null && p.Brand.BrandName.ToLower().Contains(term)) ||
                             (p.Brand != null && (p.Brand.BrandName + " " + p.ProductName).ToLower().Contains(term)));
        }

        var rawList = await q
            .OrderBy(p => p.ProductName)
            .Take(15)
            .Select(p => new ModelSuggestionDto
            {
                ProductId = p.ProductId,
                Brand = p.Brand != null ? p.Brand.BrandName : "",
                ModelName = p.ProductName,
                FullName = p.Brand != null ? $"{p.Brand.BrandName} {p.ProductName}" : p.ProductName,
                ThumbnailUrl = p.ThumbnailUrl
            })
            .ToListAsync();

        return rawList
            .GroupBy(x => x.FullName)
            .Select(g => g.First())
            .Take(8)
            .ToList();
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
