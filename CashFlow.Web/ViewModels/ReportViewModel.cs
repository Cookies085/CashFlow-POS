namespace CashFlow.Web.ViewModels;

public class ReportViewModel
{
    // Filters
    public string ReportType { get; set; } = "Sales";
    public string Period { get; set; } = "Daily";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? PaymentMethod { get; set; }

    // Summary
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalCost { get; set; }

    // Charts
    public List<ChartDataPoint> RevenueChart { get; set; } = new();
    public List<ChartDataPoint> PaymentChart { get; set; } = new();
    public List<ChartDataPoint> ProductChart { get; set; } = new();

    // Tables
    public List<ReportRow> Rows { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class ReportRow
{
    public string? Date { get; set; }
    public string? Product { get; set; }
    public string? Customer { get; set; }
    public int? Quantity { get; set; }
    public decimal? Revenue { get; set; }
    public decimal? Cost { get; set; }
    public decimal? Profit { get; set; }
    public string? PaymentMethod { get; set; }
    public string? InvoiceNumber { get; set; }
}