using CashFlow.Core.Enums;

namespace CashFlow.Core.Entities;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BusinessRegistration { get; set; }
    public string? VATNumber { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Basic;
    public DateTime? SubscriptionExpiry { get; set; }
    public int MaxUsers { get; set; } = 5;
    public int MaxStores { get; set; } = 1;
    public int MaxProducts { get; set; } = 500;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    // Navigation Properties
    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}