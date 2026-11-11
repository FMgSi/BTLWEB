using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaptopStore.Models;

public class ServiceCatalogItem
{
    [Key]
    public int ServiceId { get; set; }

    [Required]
    [StringLength(50)]
    public string ServiceCode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; } = "Sửa chữa";

    [StringLength(500)]
    public string? ApplicableConditions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCost { get; set; } = 0;

    [StringLength(100)]
    public string? EstimatedTime { get; set; }

    public int ServiceWarrantyMonths { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
