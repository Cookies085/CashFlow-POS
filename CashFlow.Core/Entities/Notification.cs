using System.ComponentModel.DataAnnotations;

namespace CashFlow.Core.Entities;

public class Notification
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? UserId { get; set; }          // Null = all users
    public int? StoreId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Warning, Danger, Success
    public bool IsRead { get; set; }
    public string? Link { get; set; }          // URL to navigate to
    public string? Reference { get; set; }    // For deduplication (e.g., movement ID)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual Store? Store { get; set; }
}