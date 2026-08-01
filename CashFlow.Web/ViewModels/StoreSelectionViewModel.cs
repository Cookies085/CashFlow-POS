using System.ComponentModel.DataAnnotations;
using CashFlow.Core.Entities;

namespace CashFlow.Web.ViewModels;

public class StoreSelectionViewModel
{
    public List<StoreViewModel> Stores { get; set; } = new();

    [Display(Name = "Store Code")]
    public string? StoreCode { get; set; }

    [Display(Name = "Remember my store")]
    public bool RememberStore { get; set; }
}

public class StoreViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? StoreCode { get; set; }
    public string? LogoUrl { get; set; }
    public string? ThemeColor { get; set; }
    public string? ThemeMode { get; set; }
    public bool IsActive { get; set; }
}