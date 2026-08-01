namespace CashFlow.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Category { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string PricingMode { get; set; } = "manual"; // manual, markup
    public decimal? MarkupPercentage { get; set; }
    public int CurrentStock { get; set; }
    public int MinStockAlert { get; set; } = 5;
    public int ReorderPoint { get; set; } = 0;
    public int? SupplierId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? StoreId { get; set; }
    public string? Barcode { get; set; }
    public decimal? Weight { get; set; }
    public string? Unit { get; set; }
    public decimal TaxRate { get; set; } = 0;
    public int? CreatedBy { get; set; }

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();
}