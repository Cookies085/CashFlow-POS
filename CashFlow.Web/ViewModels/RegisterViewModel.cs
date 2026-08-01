using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Organization name is required")]
    [Display(Name = "Organization Name")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Organization name must be between 2 and 100 characters")]
    public string OrganizationName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [Display(Name = "Full Name")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? Phone { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }
}