using System.ComponentModel.DataAnnotations;
using CashFlow.Core.Enums;

namespace CashFlow.Web.ViewModels;

public class StockMovementViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product is required")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Display(Name = "Product Name")]
    public string? ProductName { get; set; }

    [Display(Name = "Product Code")]
    public string? ProductCode { get; set; }

    [Required(ErrorMessage = "Movement type is required")]
    [Display(Name = "Movement Type")]
    public MovementType MovementType { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 999999, ErrorMessage = "Quantity must be greater than 0")]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [Display(Name = "Unit Cost")]
    [Range(0, 999999.99, ErrorMessage = "Unit cost must be 0 or greater")]
    public decimal? UnitCost { get; set; }

    [Display(Name = "Unit Price")]
    [Range(0, 999999.99, ErrorMessage = "Unit price must be 0 or greater")]
    public decimal? UnitPrice { get; set; }

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

    [Display(Name = "Store")]
    public int? StoreId { get; set; }

    [Display(Name = "Store Name")]
    public string? StoreName { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Date")]
    public DateTime CreatedAt { get; set; }

    // For display
    public string DisplayName => $"{ProductCode} - {ProductName}";
    public string MovementTypeDisplay => MovementType.ToString();
    public bool IsAddition => MovementType == MovementType.Purchase || MovementType == MovementType.Transfer;
    public int DisplayQuantity => IsAddition ? Quantity : -Quantity;
    public string QuantityColor => IsAddition ? "text-success" : "text-danger";
}

public class StockMovementListViewModel
{
    public List<StockMovementViewModel> Movements { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? MovementType { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}