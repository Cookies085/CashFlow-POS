using CashFlow.Core.Enums;

namespace CashFlow.Core.Entities;

public class User
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Cashier;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public int? StoreId { get; set; }
    public int? CreatedBy { get; set; }

    // NEW PROPERTIES
    public string? ThemePreference { get; set; }  // "light", "dark", "auto"
    public int? LastStoreId { get; set; }         // Last store user logged into

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}