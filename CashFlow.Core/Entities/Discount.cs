namespace CashFlow.Core.Entities;

public class Discount
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty; // Percentage, Fixed, BuyOneGetOne
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal MinimumPurchase { get; set; }
    public int? ProductId { get; set; }
    public string? Category { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Product? Product { get; set; }
}