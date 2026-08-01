using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class SettingsViewModel
{
    // Store Settings
    [Display(Name = "Store Name")]
    [Required(ErrorMessage = "Store name is required")]
    public string StoreName { get; set; } = string.Empty;

    [Display(Name = "Store Code")]
    public string? StoreCode { get; set; }

    [Display(Name = "Store Address")]
    public string? StoreAddress { get; set; }

    [Display(Name = "Store Phone")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? StorePhone { get; set; }

    [Display(Name = "Store Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? StoreEmail { get; set; }

    [Display(Name = "Store Logo")]
    public string? StoreLogo { get; set; }

    [Display(Name = "Upload Logo")]
    public IFormFile? LogoFile { get; set; }

    // Company Settings
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Display(Name = "Registration Number")]
    public string? RegistrationNumber { get; set; }

    [Display(Name = "VAT Number")]
    public string? VatNumber { get; set; }

    [Display(Name = "Company Address")]
    public string? CompanyAddress { get; set; }

    // Tax Settings
    [Display(Name = "VAT Rate (%)")]
    [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
    public decimal VatRate { get; set; } = 15;

    [Display(Name = "Tax Calculation Method")]
    public string TaxMethod { get; set; } = "Inclusive"; // Inclusive or Exclusive

    [Display(Name = "Tax Number Label")]
    public string? TaxNumberLabel { get; set; }

    // Receipt Settings
    [Display(Name = "Receipt Header")]
    public string? ReceiptHeader { get; set; }

    [Display(Name = "Receipt Footer")]
    public string? ReceiptFooter { get; set; }

    [Display(Name = "Show Logo on Receipt")]
    public bool ShowLogoOnReceipt { get; set; } = true;

    [Display(Name = "Receipt Paper Size")]
    public string ReceiptPaperSize { get; set; } = "80mm";

    [Display(Name = "Show Barcode on Receipt")]
    public bool ShowBarcodeOnReceipt { get; set; } = true;

    // Currency Settings
    [Display(Name = "Currency Symbol")]
    public string CurrencySymbol { get; set; } = "R";

    [Display(Name = "Currency Code")]
    public string CurrencyCode { get; set; } = "ZAR";

    [Display(Name = "Decimal Places")]
    [Range(0, 4, ErrorMessage = "Decimal places must be between 0 and 4")]
    public int DecimalPlaces { get; set; } = 2;

    [Display(Name = "Thousands Separator")]
    public string ThousandsSeparator { get; set; } = ",";

    [Display(Name = "Decimal Separator")]
    public string DecimalSeparator { get; set; } = ".";

    [Display(Name = "Currency Format")]
    public string CurrencyFormat { get; set; } = "SymbolFirst"; // SymbolFirst or SymbolLast

    // Theme Settings
    [Display(Name = "Theme Mode")]
    public string ThemeMode { get; set; } = "light";

    [Display(Name = "Primary Color")]
    public string PrimaryColor { get; set; } = "#0d5c1f";

    [Display(Name = "Accent Color")]
    public string AccentColor { get; set; } = "#22c55e";

    [Display(Name = "Sidebar Color")]
    public string SidebarColor { get; set; } = "#0d5c1f";

    // Status
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}