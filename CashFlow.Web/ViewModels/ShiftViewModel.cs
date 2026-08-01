using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class ShiftViewModel
{
    public int Id { get; set; }

    [Display(Name = "Cashier")]
    public string? CashierName { get; set; }

    [Display(Name = "Store")]
    public string? StoreName { get; set; }

    [Display(Name = "Start Time")]
    public DateTime StartTime { get; set; }

    [Display(Name = "End Time")]
    public DateTime? EndTime { get; set; }

    [Display(Name = "Opening Balance")]
    [DisplayFormat(DataFormatString = "{0:C}")]
    public decimal OpeningBalance { get; set; }

    [Display(Name = "Closing Balance")]
    [DisplayFormat(DataFormatString = "{0:C}")]
    public decimal? ClosingBalance { get; set; }

    [Display(Name = "Expected Balance")]
    [DisplayFormat(DataFormatString = "{0:C}")]
    public decimal? ExpectedBalance { get; set; }

    [Display(Name = "Difference")]
    [DisplayFormat(DataFormatString = "{0:C}")]
    public decimal? Difference { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = "Open";

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // Sales during shift
    public int TotalOrders { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCardSales { get; set; }
    public decimal TotalMobileSales { get; set; }
    public decimal TotalCreditSales { get; set; }

    [Display(Name = "Duration")]
    public string? Duration { get; set; }
}