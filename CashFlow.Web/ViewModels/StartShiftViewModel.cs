using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class StartShiftViewModel
{
    [Required(ErrorMessage = "Opening balance is required")]
    [Range(0, 999999.99, ErrorMessage = "Opening balance must be 0 or greater")]
    [Display(Name = "Opening Balance (R)")]
    public decimal OpeningBalance { get; set; }

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    public bool HasOpenShift { get; set; }
    public ShiftViewModel? OpenShift { get; set; }
}