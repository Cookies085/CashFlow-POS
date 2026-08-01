using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public int StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? StoreLogo { get; set; }
    public string? StoreThemeColor { get; set; }
}