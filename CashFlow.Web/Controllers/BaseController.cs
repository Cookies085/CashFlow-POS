using CashFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public abstract class BaseController : Controller
{
    protected IAuditService _auditService;

    protected BaseController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    protected async Task LogAuditAsync(
        string action,
        string entity,
        int? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        string? reference = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        await _auditService.LogAsync(
            userId,
            action,
            entity,
            entityId,
            oldValues,
            newValues,
            description,
            reference,
            storeId,
            organizationId,
            Request);
    }
}