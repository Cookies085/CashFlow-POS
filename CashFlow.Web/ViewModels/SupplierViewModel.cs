using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class SupplierViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Supplier name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Supplier name must be between 2 and 100 characters")]
    [Display(Name = "Supplier Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Contact Person")]
    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [Display(Name = "Phone")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Display(Name = "Address")]
    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Tax Number")]
    [StringLength(50)]
    public string? TaxNumber { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    // Display properties
    public int ProductCount { get; set; }
    public string? ProductNames { get; set; }
}