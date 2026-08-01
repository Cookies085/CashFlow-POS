namespace CashFlow.Core.Entities;

public class Setting
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? StoreId { get; set; } // NULL = Global settings

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Group { get; set; } = string.Empty; // Store, Tax, Receipt, Currency, Company, Theme

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
}