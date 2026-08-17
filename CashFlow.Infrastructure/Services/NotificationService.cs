using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task SendNotificationAsync(int organizationId, int? userId, int? storeId, string title, string message, string type, string? link = null, string? reference = null)
    {
        // Check if duplicate (same reference and title) already exists
        if (!string.IsNullOrEmpty(reference))
        {
            var existing = await _unitOfWork.Notifications
                .FirstOrDefaultAsync(n => n.Reference == reference && n.Title == title && n.OrganizationId == organizationId);
            if (existing != null)
                return;
        }

        var notification = new Notification
        {
            OrganizationId = organizationId,
            UserId = userId,
            StoreId = storeId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            Link = link,
            Reference = reference,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
    {
        var query = await _unitOfWork.Notifications
            .FindAsync(n => n.UserId == userId || n.UserId == null);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return query.OrderByDescending(n => n.CreatedAt);
    }

    public async Task<Notification?> GetNotificationByIdAsync(int id)
    {
        return await _unitOfWork.Notifications.GetByIdAsync(id);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification != null && (notification.UserId == userId || notification.UserId == null))
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _unitOfWork.Notifications
            .FindAsync(n => (n.UserId == userId || n.UserId == null) && !n.IsRead);
        foreach (var n in notifications)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            _unitOfWork.Notifications.Update(n);
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var notifications = await _unitOfWork.Notifications
            .FindAsync(n => (n.UserId == userId || n.UserId == null) && !n.IsRead);
        return notifications.Count();
    }

    public async Task DeleteOldNotificationsAsync(int daysOld = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);
        var oldNotifications = await _unitOfWork.Notifications
            .FindAsync(n => n.CreatedAt < cutoff && n.IsRead);
        foreach (var n in oldNotifications)
        {
            _unitOfWork.Notifications.Remove(n);
        }
        await _unitOfWork.SaveChangesAsync();
    }
}