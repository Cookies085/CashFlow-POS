using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class CustomerViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Address")]
    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Loyalty Points")]
    public int LoyaltyPoints { get; set; }

    [Display(Name = "Points Tier")]
    public string PointsTier { get; set; } = "Bronze";

    [Display(Name = "Total Spent")]
    public decimal TotalSpent { get; set; }

    [Display(Name = "Last Purchase")]
    public DateTime? LastPurchaseDate { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Display properties
    public string FullName => $"{FirstName} {LastName}";
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
}