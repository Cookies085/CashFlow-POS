namespace CashFlow.Core.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;

    // Navigation Properties
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}