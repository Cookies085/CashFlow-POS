namespace CashFlow.Web.ViewModels;

public class UserListViewModel
{
    public List<UserViewModel> Users { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }
}