using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;

namespace CashFlow.Web.Services;

public class ExpiryNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiryNotificationService> _logger;

    public ExpiryNotificationService(IServiceProvider serviceProvider, ILogger<ExpiryNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run at 8:00 AM daily
                var now = DateTime.UtcNow.AddHours(2); // SAST
                var nextRun = DateTime.Today.AddHours(8);
                if (nextRun < now)
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                _logger.LogInformation("ExpiryNotificationService next run at {NextRun} (in {Delay} minutes)", nextRun, delay.TotalMinutes);
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await CheckExpiryNotifications();
                    await CleanOldNotifications();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExpiryNotificationService");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task CheckExpiryNotifications()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DateTime.UtcNow.Date;

        // Get all stock movements with expiry dates and positive stock
        var movements = await unitOfWork.StockMovements
            .FindAsync(sm => sm.ExpiryDate.HasValue && sm.Quantity > 0);

        var thresholds = new[] { 30, 14, 7, 3, 1, 0 };

        foreach (var movement in movements)
        {
            var expiryDate = movement.ExpiryDate.Value.Date;
            var daysUntilExpiry = (expiryDate - today).Days;

            if (daysUntilExpiry < 0) continue;

            var product = await unitOfWork.Products.GetByIdAsync(movement.ProductId);
            if (product == null) continue;

            foreach (var threshold in thresholds)
            {
                if (daysUntilExpiry == threshold)
                {
                    var title = threshold == 0 ? "⚠️ Product Expired" : $"📦 Product Expiring Soon";
                    var message = threshold == 0
                        ? $"{product.ItemName} (Batch: {movement.BatchNumber ?? "N/A"}) has expired. Please dispose or return."
                        : $"{product.ItemName} (Batch: {movement.BatchNumber ?? "N/A"}) will expire in {daysUntilExpiry} day{(daysUntilExpiry > 1 ? "s" : "")}.";

                    var reference = $"{movement.Id}-{threshold}";
                    await notificationService.SendNotificationAsync(
                        organizationId: product.OrganizationId,
                        userId: null,
                        storeId: movement.StoreId,
                        title: title,
                        message: message,
                        type: threshold == 0 ? "Danger" : "Warning",
                        link: $"/Products/Details/{product.Id}",
                        reference: reference
                    );
                    break;
                }
            }
        }
    }

    private async Task CleanOldNotifications()
    {
        using var scope = _serviceProvider.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notificationService.DeleteOldNotificationsAsync(30);
    }
}