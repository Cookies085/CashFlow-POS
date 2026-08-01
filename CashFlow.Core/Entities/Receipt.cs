namespace CashFlow.Core.Entities;

public class Receipt
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceiptContent { get; set; } = string.Empty;
    public DateTime PrintedAt { get; set; } = DateTime.UtcNow;
    public int? PrintedBy { get; set; }
    public string ReceiptType { get; set; } = "Sale"; // Sale, Refund, Credit
    public bool IsReprint { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual User? PrintedByUser { get; set; }
}