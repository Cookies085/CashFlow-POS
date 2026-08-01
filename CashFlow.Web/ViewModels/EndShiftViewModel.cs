using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class EndShiftViewModel
{
    public int ShiftId { get; set; }

    [Required(ErrorMessage = "Closing balance is required")]
    [Range(0, 999999.99, ErrorMessage = "Closing balance must be 0 or greater")]
    [Display(Name = "Closing Balance (R)")]
    public decimal ClosingBalance { get; set; }

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    // Summary information
    public ShiftViewModel? Shift { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCardSales { get; set; }
    public decimal TotalMobileSales { get; set; }
    public decimal TotalCreditSales { get; set; }
    public int TotalOrders { get; set; }
}