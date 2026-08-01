using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class AuditController : Controller
{
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public AuditController(IAuditService auditService, IUnitOfWork unitOfWork)
    {
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    // GET: Audit
    public async Task<IActionResult> Index(
        string? searchTerm,
        string? actionFilter,
        string? entityFilter,
        int? userIdFilter,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        // Check if user has permission (only SuperAdmin, Admin, and Manager can view audit logs)
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "SuperAdmin" && userRole != "Admin" && userRole != "Manager")
        {
            TempData["ErrorMessage"] = "You don't have permission to view audit logs.";
            return RedirectToAction("Index", "Home");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get audit logs with filters
        var logs = await _auditService.GetAuditLogsAsync(
            organizationId,
            storeId,
            actionFilter,
            entityFilter,
            userIdFilter,
            dateFrom,
            dateTo,
            searchTerm);

        // Get users for filter dropdown
        var users = await _unitOfWork.Users
            .FindAsync(u => u.OrganizationId == organizationId);

        // Get unique actions and entities for filters
        var allLogs = await _auditService.GetAuditLogsAsync(organizationId);
        var actions = allLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToList();
        var entities = allLogs.Select(l => l.Entity).Distinct().OrderBy(e => e).ToList();

        // Convert to ViewModel
        var auditViewModels = logs.Select(l => new AuditViewModel
        {
            Id = l.Id,
            UserName = l.User?.Username ?? "Unknown",
            UserFullName = l.User?.FullName ?? "Unknown",
            Action = l.Action,
            Entity = l.Entity,
            EntityId = l.EntityId,
            Description = l.Description,
            Timestamp = l.Timestamp,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent,
            Reference = l.Reference,
            OldValues = l.OldValues,
            NewValues = l.NewValues
        }).ToList();

        // Pagination
        int pageSize = 20;
        int totalCount = auditViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedAudits = auditViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new AuditListViewModel
        {
            Audits = pagedAudits,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            ActionFilter = actionFilter,
            EntityFilter = entityFilter,
            UserIdFilter = userIdFilter,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        ViewBag.Actions = actions;
        ViewBag.Entities = entities;
        ViewBag.Users = users.OrderBy(u => u.FullName).ToList();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentAction = actionFilter;
        ViewBag.CurrentEntity = entityFilter;
        ViewBag.CurrentUserId = userIdFilter;

        return View(model);
    }

    // GET: Audit/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var log = await _unitOfWork.AuditLogs.GetByIdAsync(id);
        if (log == null)
        {
            return NotFound();
        }

        var model = new AuditViewModel
        {
            Id = log.Id,
            UserName = log.User?.Username ?? "Unknown",
            UserFullName = log.User?.FullName ?? "Unknown",
            Action = log.Action,
            Entity = log.Entity,
            EntityId = log.EntityId,
            Description = log.Description,
            Timestamp = log.Timestamp,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Reference = log.Reference,
            OldValues = log.OldValues,
            NewValues = log.NewValues
        };

        return View(model);
    }

    // GET: Audit/Export
    public async Task<IActionResult> Export(
        string? searchTerm,
        string? actionFilter,
        string? entityFilter,
        int? userIdFilter,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var logs = await _auditService.GetAuditLogsAsync(
            organizationId,
            storeId,
            actionFilter,
            entityFilter,
            userIdFilter,
            dateFrom,
            dateTo,
            searchTerm);

        // Build a simple text export
        var lines = new List<string>
        {
            "Audit Log Export",
            "================",
            $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}",
            $"Total Records: {logs.Count()}",
            "",
            "Timestamp | User | Action | Entity | Description | IP Address | Reference"
        };

        lines.Add(new string('-', 100));

        foreach (var log in logs)
        {
            lines.Add($"{log.Timestamp:dd MMM yyyy HH:mm} | {log.User?.FullName} | {log.Action} | {log.Entity} | {log.Description} | {log.IpAddress} | {log.Reference}");
        }

        var content = string.Join(Environment.NewLine, lines);
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        return File(bytes, "text/plain", $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    }
}