namespace CashFlow.Core.Entities;

public class Customer
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int LoyaltyPoints { get; set; }
    public string PointsTier { get; set; } = "Bronze";
    public decimal TotalSpent { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public int? StoreId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
    public virtual ICollection<GiftCard> GiftCards { get; set; } = new List<GiftCard>();
    public virtual ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();
}