using System.ComponentModel.DataAnnotations;

namespace CashFlow.Core.Entities;

public class Store
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    // NEW PROPERTIES
    public string? StoreCode { get; set; }  // e.g., "JHB001", "CPT002"
    public string? LogoUrl { get; set; }    // Path to store logo
    public string? ThemeColor { get; set; } // Primary color (hex)
    public string? ThemeMode { get; set; }  // "light", "dark", "auto"
    public string? Timezone { get; set; }   // e.g., "Africa/Johannesburg"

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}