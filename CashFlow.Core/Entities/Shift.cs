namespace CashFlow.Core.Entities;

public class Shift
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    public int? StoreId { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }

    public string Status { get; set; } = "Open"; // Open, Closed
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Store? Store { get; set; }
}