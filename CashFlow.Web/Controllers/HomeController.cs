using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class HomeController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        // Check if user is logged in
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        // Check if user has an open shift
        var openShift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Status == "Open");

        if (openShift == null)
        {
            // No open shift - redirect to shift start prompt
            return RedirectToAction("Prompt", "Shifts");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var model = new DashboardViewModel();

        // Get today's date range (South Africa time)
        var today = DateTime.UtcNow.AddHours(2); // SAST
        var todayStart = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-2);
        var todayEnd = todayStart.AddDays(1).AddSeconds(-1);

        // Get month start
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-2);

        // Filter sales by organization
        var allSales = await _unitOfWork.Sales
            .FindAsync(s => s.OrganizationId == organizationId);

        // Filter by store if applicable
        if (storeId.HasValue)
        {
            allSales = allSales.Where(s => s.StoreId == storeId.Value).ToList();
        }

        // Today's sales
        var todaySales = allSales
            .Where(s => s.SaleDate >= todayStart && s.SaleDate <= todayEnd && s.PaymentStatus == "Paid")
            .ToList();

        model.TodaySales = todaySales.Sum(s => s.NetAmount);
        model.TodayOrders = todaySales.Count();

        // Monthly sales
        var monthlySales = allSales
            .Where(s => s.SaleDate >= monthStart && s.PaymentStatus == "Paid")
            .ToList();

        model.MonthlySales = monthlySales.Sum(s => s.NetAmount);

        // Total revenue (all time)
        model.TotalRevenue = allSales
            .Where(s => s.PaymentStatus == "Paid")
            .Sum(s => s.NetAmount);

        // Product stats
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId);

        if (storeId.HasValue)
        {
            products = products.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
        }

        model.TotalProducts = products.Count();
        model.LowStockItems = products.Count(p => p.CurrentStock <= p.MinStockAlert && p.CurrentStock > 0);
        model.OutOfStockItems = products.Count(p => p.CurrentStock <= 0);

        // Customer stats
        var customers = await _unitOfWork.Customers
            .FindAsync(c => c.OrganizationId == organizationId);

        if (storeId.HasValue)
        {
            customers = customers.Where(c => c.StoreId == storeId.Value || c.StoreId == null);
        }

        model.TotalCustomers = customers.Count();
        model.TotalLoyaltyPoints = customers.Sum(c => c.LoyaltyPoints);
        model.TotalCustomersWithPoints = customers.Count(c => c.LoyaltyPoints > 0);

        // Recent sales (last 10)
        var recentSales = allSales
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .ToList();

        model.RecentSales = recentSales.Select(s => new RecentSaleViewModel
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            CustomerName = s.CustomerId.HasValue ?
                $"{s.Customer?.FirstName} {s.Customer?.LastName}" :
                "Walk-in Customer",
            SaleDate = s.SaleDate,
            TotalAmount = s.NetAmount,
            PaymentMethod = s.PaymentMethod.ToString(),
            Status = s.PaymentStatus
        }).ToList();

        // Top products (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Get sale items with product info
        var saleItems = await _unitOfWork.SaleItems
            .FindAsync(si => si.Sale.OrganizationId == organizationId &&
                            si.CreatedAt >= thirtyDaysAgo);

        if (storeId.HasValue)
        {
            saleItems = saleItems.Where(si => si.Sale.StoreId == storeId.Value);
        }

        var topProducts = saleItems
            .GroupBy(si => new { si.ProductId, si.Product.ItemName, si.Product.ItemCode })
            .Select(g => new TopProductViewModel
            {
                ProductId = g.Key.ProductId,
                ItemName = g.Key.ItemName,
                ItemCode = g.Key.ItemCode,
                TotalQuantity = g.Sum(si => si.Quantity),
                TotalRevenue = g.Sum(si => si.Subtotal)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToList();

        model.TopProducts = topProducts;

        // Sales by payment method
        var salesByPayment = allSales
            .Where(s => s.PaymentStatus == "Paid")
            .GroupBy(s => s.PaymentMethod.ToString())
            .Select(g => new { Method = g.Key, Total = g.Sum(s => s.NetAmount) })
            .ToDictionary(x => x.Method, x => x.Total);

        model.SalesByPaymentMethod = salesByPayment;

        // Sales chart data (last 7 days)
        var chartData = new ChartDataViewModel();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var dayStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc).AddHours(-2);
            var dayEnd = dayStart.AddDays(1).AddSeconds(-1);

            var daySales = allSales
                .Where(s => s.SaleDate >= dayStart && s.SaleDate <= dayEnd && s.PaymentStatus == "Paid")
                .Sum(s => s.NetAmount);

            chartData.Labels.Add(date.ToString("ddd"));
            chartData.Values.Add(daySales);
        }

        model.SalesChart = chartData;

        // =============================================
        // EXPIRY ALERTS - ADD THIS SECTION
        // =============================================
        var expiryMovements = await _unitOfWork.StockMovements
            .FindAsync(sm => sm.ExpiryDate.HasValue && sm.Quantity > 0);

        if (storeId.HasValue)
        {
            expiryMovements = expiryMovements.Where(sm => sm.StoreId == storeId.Value || sm.StoreId == null);
        }

        var expiringItems = new List<ExpiryAlertViewModel>();
        foreach (var movement in expiryMovements)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(movement.ProductId);
            if (product == null || !product.IsActive) continue;

            var days = (movement.ExpiryDate.Value.Date - DateTime.UtcNow.Date).Days;
            if (days < 0)
            {
                model.ExpiredCount++;
            }
            else if (days <= 30)
            {
                model.ExpiringCount++;
                expiringItems.Add(new ExpiryAlertViewModel
                {
                    ProductId = product.Id,
                    ItemName = product.ItemName,
                    ItemCode = product.ItemCode,
                    DaysUntilExpiry = days,
                    ExpiryDate = movement.ExpiryDate.Value,
                    BatchNumber = movement.BatchNumber ?? "N/A",
                    Quantity = movement.Quantity
                });
            }
        }
        model.HasExpiryAlerts = model.ExpiredCount > 0 || model.ExpiringCount > 0;
        model.UrgentExpiries = expiringItems.OrderBy(e => e.DaysUntilExpiry).Take(5).ToList();

        ViewBag.StoreName = HttpContext.Session.GetString("StoreName") ?? "Main Store";
        ViewBag.UserName = HttpContext.Session.GetString("UserFullName") ?? "User";

        return View(model);
    }

    public async Task<IActionResult> Privacy()
    {
        await LogAuditAsync(
            "View",
            "Privacy",
            null,
            null,
            null,
            "User viewed privacy policy",
            null);

        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}