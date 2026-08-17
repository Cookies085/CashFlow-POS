using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class NotificationsController : BaseController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService, IAuditService auditService)
        : base(auditService)
    {
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        await _notificationService.MarkAsReadAsync(id, userId);
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        await _notificationService.MarkAllAsReadAsync(userId);
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> GetLatest(int count = 10)
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, true);
        return PartialView("_NotificationList", notifications.Take(count));
    }
}