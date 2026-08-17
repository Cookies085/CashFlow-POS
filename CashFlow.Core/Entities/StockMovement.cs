using CashFlow.Core.Enums;

namespace CashFlow.Core.Entities;

public class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? UnitPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? BatchNumber { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public int? StoreId { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // NEW PROPERTIES FOR ENHANCED RECEIVING
    public string? SourceType { get; set; }                 // Purchase, Transfer, Return, Adjustment, Donation, Consignment
    public string? ReceiptImageUrl { get; set; }            // Path to uploaded receipt image
    public string? ReceiptNumber { get; set; }              // Supplier invoice/order number
    public decimal? PreviousUnitCost { get; set; }          // Cost before this movement
    public decimal? PreviousSellingPrice { get; set; }      // Price before this movement
    public bool CostIncreased { get; set; }                 // True if unit cost increased
    public bool IsValidated { get; set; }                   // True if receipt validated
    public int? ValidatedBy { get; set; }                   // User who validated
    public DateTime? ValidatedAt { get; set; }              // Validation date
    public string? ValidationNotes { get; set; }            // Notes about validation

    // Navigation Properties
    public virtual Product Product { get; set; } = null!;
    public virtual Store? Store { get; set; }
}