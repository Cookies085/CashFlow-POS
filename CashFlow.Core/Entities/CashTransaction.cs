namespace CashFlow.Core.Entities;

public class CashTransaction
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? StoreId { get; set; }
    public int CashAccountId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Debit, Credit
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual CashAccount CashAccount { get; set; } = null!;
}