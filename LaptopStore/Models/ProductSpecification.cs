using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LaptopStore.Models;

namespace LaptopStore.Models;

public class ProductSpecification
{
    [Key, ForeignKey("Product")]
    public int ProductId { get; set; }
    public string? CPU { get; set; }
    public int RamGB { get; set; }
    public int StorageGB { get; set; }
    public string? StorageType { get; set; }
    public string? GPU { get; set; }
    public decimal ScreenSizeInch { get; set; }
    public int RefreshRateHz { get; set; }
    public decimal WeightKg { get; set; }
    public int BatteryWh { get; set; }
    public string? OS { get; set; }
    public Product? Product { get; set; }
}

