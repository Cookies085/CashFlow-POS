namespace CashFlow.Web.ViewModels;

public class SalesListViewModel
{
    public List<SaleViewModel> Sales { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
}

public class SaleViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}