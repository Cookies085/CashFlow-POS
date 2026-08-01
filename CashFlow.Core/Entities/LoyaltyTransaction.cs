using CashFlow.Core.Enums;

namespace CashFlow.Core.Entities;

public class LoyaltyTransaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? SaleId { get; set; }
    public int PointsEarned { get; set; }
    public int PointsRedeemed { get; set; }
    public int Balance { get; set; }
    public TransactionType TransactionType { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Sale? Sale { get; set; }
}