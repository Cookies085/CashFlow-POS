namespace CashFlow.Web.ViewModels;

public class CustomerListViewModel
{
    public IEnumerable<CustomerViewModel> Customers { get; set; } = new List<CustomerViewModel>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? TierFilter { get; set; }
}