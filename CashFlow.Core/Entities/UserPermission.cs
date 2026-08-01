namespace CashFlow.Core.Entities;

public class UserPermission
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}