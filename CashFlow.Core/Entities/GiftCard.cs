namespace CashFlow.Core.Entities;

public class GiftCard
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public decimal Balance { get; set; }
    public decimal OriginalAmount { get; set; }
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? StoreId { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual Store? Store { get; set; }
}