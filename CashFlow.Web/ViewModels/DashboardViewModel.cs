namespace CashFlow.Web.ViewModels;

public class DashboardViewModel
{
    // Summary Cards
    public decimal TodaySales { get; set; }
    public decimal MonthlySales { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }

    // Recent Sales
    public List<RecentSaleViewModel> RecentSales { get; set; } = new();

    // Sales Chart Data
    public ChartDataViewModel SalesChart { get; set; } = new();

    // Top Products
    public List<TopProductViewModel> TopProducts { get; set; } = new();

    // Sales by Payment Method
    public Dictionary<string, decimal> SalesByPaymentMethod { get; set; } = new();

    // Customer Loyalty Stats
    public int TotalLoyaltyPoints { get; set; }
    public int TotalCustomersWithPoints { get; set; }

    // Alerts
    public bool HasLowStock => LowStockItems > 0;
    public bool HasOutOfStock => OutOfStockItems > 0;
}

public class RecentSaleViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
    public List<decimal> TargetValues { get; set; } = new();
}

public class TopProductViewModel
{
    public int ProductId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}