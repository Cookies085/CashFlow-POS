namespace CashFlow.Web.ViewModels;

public class AuditListViewModel
{
    public List<AuditViewModel> Audits { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? ActionFilter { get; set; }
    public string? EntityFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? UserIdFilter { get; set; }
}

public class AuditViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reference { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public bool HasChanges => !string.IsNullOrEmpty(OldValues) && !string.IsNullOrEmpty(NewValues);
    public string DisplayName => $"{Action} {Entity}";
    public string ActionBadgeColor => Action switch
    {
        "Login" => "bg-success",
        "Logout" => "bg-secondary",
        "Create" => "bg-primary",
        "Edit" => "bg-warning",
        "Delete" => "bg-danger",
        "Void" => "bg-danger",
        "View" => "bg-info",
        _ => "bg-secondary"
    };
}