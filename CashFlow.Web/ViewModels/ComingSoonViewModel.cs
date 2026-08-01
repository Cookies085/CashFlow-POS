namespace CashFlow.Web.ViewModels;

public class ComingSoonViewModel
{
    public string FeatureName { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-rocket";
    public string Description { get; set; } = string.Empty;
    public DateTime? EstimatedRelease { get; set; }
    public List<string> Features { get; set; } = new();
    public string Color { get; set; } = "success";
}