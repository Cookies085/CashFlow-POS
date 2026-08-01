using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class POSViewModel
{
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public int? LoyaltyPoints { get; set; }

    public List<CartItemViewModel> CartItems { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public decimal ChangeAmount { get; set; }

    public string? Notes { get; set; }
    public bool IsLoyaltyApplied { get; set; }
    public int PointsToRedeem { get; set; }
    public int PointsEarned { get; set; }
}

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
}