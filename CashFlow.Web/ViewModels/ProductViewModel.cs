using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Item code is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Item code must be between 2 and 50 characters")]
    [Display(Name = "Item Code")]
    public string ItemCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Item name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Item name must be between 2 and 200 characters")]
    [Display(Name = "Item Name")]
    public string ItemName { get; set; } = string.Empty;

    [Display(Name = "Size")]
    [StringLength(50)]
    public string? Size { get; set; }

    [Display(Name = "Category")]
    [StringLength(100)]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Cost price is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Cost price must be greater than 0")]
    [Display(Name = "Cost Price (R)")]
    public decimal CostPrice { get; set; }

    [Required(ErrorMessage = "Selling price is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Selling price must be greater than 0")]
    [Display(Name = "Selling Price (R)")]
    public decimal SellingPrice { get; set; }

    [Display(Name = "Pricing Mode")]
    public string PricingMode { get; set; } = "manual";

    [Display(Name = "Markup Percentage")]
    [Range(0, 1000, ErrorMessage = "Markup must be between 0 and 1000%")]
    public decimal? MarkupPercentage { get; set; }

    [Display(Name = "Current Stock")]
    [Range(0, 999999, ErrorMessage = "Stock must be 0 or greater")]
    public int CurrentStock { get; set; }

    [Display(Name = "Min Stock Alert")]
    [Range(0, 999999, ErrorMessage = "Min stock must be 0 or greater")]
    public int MinStockAlert { get; set; } = 5;

    [Display(Name = "Reorder Point")]
    [Range(0, 999999, ErrorMessage = "Reorder point must be 0 or greater")]
    public int ReorderPoint { get; set; } = 0;

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Barcode")]
    [StringLength(50)]
    public string? Barcode { get; set; }

    [Display(Name = "Weight (kg)")]
    [Range(0, 999999.99, ErrorMessage = "Weight must be 0 or greater")]
    public decimal? Weight { get; set; }

    [Display(Name = "Unit")]
    [StringLength(20)]
    public string? Unit { get; set; }

    [Display(Name = "Tax Rate (%)")]
    [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100")]
    public decimal TaxRate { get; set; } = 0;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // For display purposes
    public string? SupplierName { get; set; }
    public string? StoreName { get; set; }
}