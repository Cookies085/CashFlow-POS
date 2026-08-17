namespace CashFlow.Core.Entities;

public class CashAccount
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? StoreId { get; set; }
    public string AccountName { get; set; } = "Petty Cash";
    public decimal Balance { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
}