namespace CashFlow.Core.Entities;

public class CustomerPayment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? SaleId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentType { get; set; } = string.Empty; // Credit, Payment
    public decimal Balance { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Sale? Sale { get; set; }
}