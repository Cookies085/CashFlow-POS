using System.ComponentModel.DataAnnotations;

namespace CashFlow.Web.ViewModels;

public class StockCountViewModel
{
    public int Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public DateTime CountDate { get; set; }
    public string Status { get; set; } = "In Progress";
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }
    public int TotalItems { get; set; }
    public int ItemsWithDiscrepancy { get; set; }
    public int TotalDiscrepancy { get; set; }
    public string StoreName { get; set; } = string.Empty;
}

public class StockCountItemViewModel
{
    public int Id { get; set; }
    public int StockCountId { get; set; }
    public int ProductId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Category { get; set; }
    public int SystemStock { get; set; }
    public int ActualStock { get; set; }
    public int Difference { get; set; }
    public string? Notes { get; set; }
    public bool HasDiscrepancy => Difference != 0;
    public string DifferenceDisplay => Difference > 0 ? $"+{Difference}" : Difference.ToString();
    public string DifferenceColor => Difference > 0 ? "text-danger" : Difference < 0 ? "text-success" : "text-muted";
}

public class StockCountListViewModel
{
    public List<StockCountViewModel> StockCounts { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
}

public class StartStockCountViewModel
{
    public List<ProductSelectViewModel> Products { get; set; } = new();
    public string? Notes { get; set; }
}

public class ProductSelectViewModel
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Category { get; set; }
    public int SystemStock { get; set; }
    public int? ActualStock { get; set; }
    public bool IsCounted { get; set; }
}

public class StockCountSummaryViewModel
{
    public int TotalCounted { get; set; }
    public int TotalItems { get; set; }
    public int ItemsWithDiscrepancy { get; set; }
    public int TotalOver { get; set; }
    public int TotalShort { get; set; }
    public decimal PercentageComplete { get; set; }
}