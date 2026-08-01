namespace CashFlow.Core.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty; // Login, Logout, Create, Edit, Delete, View, Void, etc.
    public string Entity { get; set; } = string.Empty; // Product, Sale, Customer, User, etc.
    public int? EntityId { get; set; }
    public string? OldValues { get; set; } // JSON of old values
    public string? NewValues { get; set; } // JSON of new values
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reference { get; set; } // Invoice number, etc.
    public int? StoreId { get; set; }
    public int? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual Organization? Organization { get; set; }
}