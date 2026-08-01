using CashFlow.Core.Enums;

namespace CashFlow.Core.Entities;

public class Sale
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = "Paid";
    public int UserId { get; set; }
    public int? StoreId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Customer? Customer { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();
    public virtual ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
}