using CashFlow.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CashFlow.Web.Filters;

public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly UserRole[] _allowedRoles;

    public AuthorizeRoleAttribute(params UserRole[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userRole = context.HttpContext.Session.GetString("UserRole");
        var userId = context.HttpContext.Session.GetInt32("UserId");

        // Check if user is logged in
        if (!userId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // If no roles specified, allow all authenticated users
        if (_allowedRoles == null || _allowedRoles.Length == 0)
        {
            return;
        }

        // Check if user has one of the allowed roles
        if (string.IsNullOrEmpty(userRole) || !Enum.TryParse<UserRole>(userRole, out var role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
            return;
        }

        if (!_allowedRoles.Contains(role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
        }
    }
}

public class AuthorizePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public AuthorizePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userId = context.HttpContext.Session.GetInt32("UserId");
        var userRole = context.HttpContext.Session.GetString("UserRole");

        // Check if user is logged in
        if (!userId.HasValue)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // SuperAdmin has all permissions
        if (userRole == UserRole.SuperAdmin.ToString())
        {
            return;
        }

        // TODO: Check granular permissions from database
        // For now, use role-based permission mapping
        var hasPermission = HasPermission(userRole, _permission);

        if (!hasPermission)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
        }
    }

    private bool HasPermission(string? userRole, string permission)
    {
        if (string.IsNullOrEmpty(userRole))
            return false;

        // SuperAdmin has all permissions
        if (userRole == nameof(UserRole.SuperAdmin))
            return true;

        // Admin has all permissions
        if (userRole == nameof(UserRole.Admin))
            return true;

        // Role-based permission mapping
        return (userRole, permission) switch
        {
            // Manager permissions
            (nameof(UserRole.Manager), "ViewProducts") => true,
            (nameof(UserRole.Manager), "EditProducts") => true,
            (nameof(UserRole.Manager), "ViewSales") => true,
            (nameof(UserRole.Manager), "ProcessSales") => true,
            (nameof(UserRole.Manager), "ViewCustomers") => true,
            (nameof(UserRole.Manager), "EditCustomers") => true,
            (nameof(UserRole.Manager), "ViewReports") => true,
            (nameof(UserRole.Manager), "ViewStock") => true,
            (nameof(UserRole.Manager), "ManageStock") => true,

            // Cashier permissions
            (nameof(UserRole.Cashier), "ViewProducts") => true,
            (nameof(UserRole.Cashier), "ProcessSales") => true,
            (nameof(UserRole.Cashier), "ViewCustomers") => true,
            (nameof(UserRole.Cashier), "ViewStock") => true,

            // Viewer permissions
            (nameof(UserRole.Viewer), "ViewProducts") => true,
            (nameof(UserRole.Viewer), "ViewSales") => true,
            (nameof(UserRole.Viewer), "ViewCustomers") => true,
            (nameof(UserRole.Viewer), "ViewReports") => true,
            (nameof(UserRole.Viewer), "ViewStock") => true,

            _ => false
        };
    }
}