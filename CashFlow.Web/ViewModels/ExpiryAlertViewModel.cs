namespace CashFlow.Web.ViewModels;

public class ExpiryAlertViewModel
{
    public int ProductId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Status { get; set; } = string.Empty;
}