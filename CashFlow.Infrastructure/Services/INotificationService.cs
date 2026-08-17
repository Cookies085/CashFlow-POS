using CashFlow.Core.Entities;

namespace CashFlow.Infrastructure.Services;

public interface INotificationService
{
    Task SendNotificationAsync(int organizationId, int? userId, int? storeId, string title, string message, string type, string? link = null, string? reference = null);
    Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);
    Task<Notification?> GetNotificationByIdAsync(int id);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task DeleteOldNotificationsAsync(int daysOld = 30);
}