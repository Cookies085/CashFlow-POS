using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class StockReceiveViewModel
{
    [Required(ErrorMessage = "Product is required")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    // Auto-filled fields
    [Display(Name = "Product Name")]
    public string? ProductName { get; set; }

    [Display(Name = "Current Cost Price")]
    public decimal? CurrentCostPrice { get; set; }

    [Display(Name = "Current Selling Price")]
    public decimal? CurrentSellingPrice { get; set; }

    [Display(Name = "Barcode")]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 999999, ErrorMessage = "Quantity must be greater than 0")]
    [Display(Name = "Quantity Received")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Unit cost is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Unit cost must be greater than 0")]
    [Display(Name = "Unit Cost (R)")]
    public decimal UnitCost { get; set; }

    [Display(Name = "Selling Price (R)")]
    [Range(0, 999999.99, ErrorMessage = "Selling price must be 0 or greater")]
    public decimal? UnitPrice { get; set; }

    [Display(Name = "Source Type")]
    public string SourceType { get; set; } = "Purchase";

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Receipt Number")]
    [StringLength(50)]
    public string? ReceiptNumber { get; set; }

    [Display(Name = "Upload Receipt")]
    public IFormFile? ReceiptFile { get; set; }

    [Display(Name = "Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Display(Name = "Batch Number")]
    [StringLength(50)]
    public string? BatchNumber { get; set; }

    [Display(Name = "Reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    // Receipt validation fields
    [Display(Name = "Receipt Quantity")]
    [Range(0, 999999)]
    public int? ReceiptQuantity { get; set; }

    [Display(Name = "Receipt Total (R)")]
    [Range(0, 999999.99)]
    public decimal? ReceiptTotal { get; set; }

    // Cash account
    [Display(Name = "Cash Account")]
    public int? CashAccountId { get; set; }

    // Calculated properties
    public decimal TotalCost => Quantity * UnitCost;
    public bool IsCostIncreased { get; set; }
    public decimal CostDifference { get; set; }
    public decimal CostIncreasePercentage { get; set; }
    public bool IsReceiptValid => ReceiptQuantity.HasValue && ReceiptTotal.HasValue &&
                                  Quantity == ReceiptQuantity.Value &&
                                  Math.Abs(TotalCost - ReceiptTotal.Value) < 0.01m;
}