using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class StockReceiveViewModel
{
    [Required(ErrorMessage = "Product is required")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 999999, ErrorMessage = "Quantity must be greater than 0")]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Unit cost is required")]
    [Range(0.01, 999999.99, ErrorMessage = "Unit cost must be greater than 0")]
    [Display(Name = "Unit Cost (R)")]
    public decimal UnitCost { get; set; }

    [Display(Name = "Unit Price (R)")]
    [Range(0, 999999.99, ErrorMessage = "Unit price must be 0 or greater")]
    public decimal? UnitPrice { get; set; }

    [Display(Name = "Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Display(Name = "Batch Number")]
    [StringLength(50)]
    public string? BatchNumber { get; set; }

    [Display(Name = "Supplier")]
    public int? SupplierId { get; set; }

    [Display(Name = "Reference")]
    [StringLength(100)]
    public string? Reference { get; set; }

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }
}