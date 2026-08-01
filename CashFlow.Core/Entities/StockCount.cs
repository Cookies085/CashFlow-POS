using System.ComponentModel.DataAnnotations;

namespace CashFlow.Core.Entities;

public class StockCount
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? StoreId { get; set; }
    public string CountNumber { get; set; } = string.Empty; // e.g., SC-2025-001
    public DateTime CountDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "In Progress"; // In Progress, Completed, Verified
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual User? CreatedByUser { get; set; }
    public virtual User? VerifiedByUser { get; set; }
    public virtual ICollection<StockCountItem> StockCountItems { get; set; } = new List<StockCountItem>();
}

public class StockCountItem
{
    public int Id { get; set; }
    public int StockCountId { get; set; }
    public int ProductId { get; set; }
    public int SystemStock { get; set; }  // What the system says
    public int ActualStock { get; set; }   // What was counted
    public int Difference => ActualStock - SystemStock; // Calculated
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual StockCount StockCount { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}