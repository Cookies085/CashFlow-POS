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

    // Navigation Properties
    public virtual Product Product { get; set; } = null!;
    public virtual Store? Store { get; set; }
}