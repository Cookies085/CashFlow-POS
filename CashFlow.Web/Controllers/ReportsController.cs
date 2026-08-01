using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CashFlow.Web.Controllers;

public class ReportsController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Reports
    public async Task<IActionResult> Index()
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Default to this month
        var today = DateTime.UtcNow.AddHours(2);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var model = new ReportViewModel
        {
            ReportType = "Sales",
            Period = "Monthly",
            DateFrom = monthStart,
            DateTo = today,
            PaymentMethod = null
        };

        // Load data based on filters
        await LoadReportData(model, organizationId, storeId);

        // Get payment methods for filter
        var paymentMethods = Enum.GetValues(typeof(PaymentMethod))
            .Cast<PaymentMethod>()
            .Select(p => p.ToString())
            .ToList();

        ViewBag.PaymentMethods = paymentMethods;
        ViewBag.ReportTypes = new List<string> { "Sales", "Products", "Customers", "Profit" };
        ViewBag.Periods = new List<string> { "Daily", "Weekly", "Monthly", "Yearly" };

        // Log view reports
        await LogAuditAsync(
            "View",
            "Reports",
            null,
            null,
            null,
            $"Viewed reports page. Report Type: Sales, Period: Monthly",
            null);

        return View(model);
    }

    // POST: Reports/Index
    [HttpPost]
    public async Task<IActionResult> Index(ReportViewModel model)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Load data based on filters
        await LoadReportData(model, organizationId, storeId);

        // Get payment methods for filter
        var paymentMethods = Enum.GetValues(typeof(PaymentMethod))
            .Cast<PaymentMethod>()
            .Select(p => p.ToString())
            .ToList();

        ViewBag.PaymentMethods = paymentMethods;
        ViewBag.ReportTypes = new List<string> { "Sales", "Products", "Customers", "Profit" };
        ViewBag.Periods = new List<string> { "Daily", "Weekly", "Monthly", "Yearly" };

        // Log report generation
        await LogAuditAsync(
            "Generate",
            "Report",
            null,
            null,
            model,
            $"Generated {model.ReportType} report. Period: {model.Period}, Revenue: {model.TotalRevenue:C}, Orders: {model.TotalOrders}",
            null);

        return View(model);
    }

    private async Task LoadReportData(ReportViewModel model, int organizationId, int? storeId)
    {
        // Get all sales for the organization
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.OrganizationId == organizationId && s.PaymentStatus == "Paid");

        // Filter by store
        if (storeId.HasValue)
        {
            sales = sales.Where(s => s.StoreId == storeId.Value);
        }

        // Apply date filters
        if (model.DateFrom.HasValue)
        {
            sales = sales.Where(s => s.SaleDate >= model.DateFrom.Value);
        }
        if (model.DateTo.HasValue)
        {
            var endDate = model.DateTo.Value.AddDays(1);
            sales = sales.Where(s => s.SaleDate <= endDate);
        }

        // Apply payment method filter
        if (!string.IsNullOrEmpty(model.PaymentMethod))
        {
            sales = sales.Where(s => s.PaymentMethod.ToString() == model.PaymentMethod);
        }

        // Get sale items for profit calculation
        var saleItems = new List<CashFlow.Core.Entities.SaleItem>();
        foreach (var sale in sales)
        {
            var items = await _unitOfWork.SaleItems.FindAsync(si => si.SaleId == sale.Id);
            saleItems.AddRange(items);
        }

        // Calculate totals
        model.TotalRevenue = sales.Sum(s => s.NetAmount);
        model.TotalOrders = sales.Count();
        model.TotalTax = sales.Sum(s => s.TaxAmount);
        model.TotalDiscount = sales.Sum(s => s.DiscountAmount);
        model.AverageOrderValue = model.TotalOrders > 0 ? model.TotalRevenue / model.TotalOrders : 0;
        model.TotalCost = saleItems.Sum(si => si.CostPriceAtSale * si.Quantity);
        model.TotalProfit = model.TotalRevenue - model.TotalCost;

        // Build chart data based on report type
        await BuildChartData(model, sales, saleItems);

        // Build table rows based on report type
        await BuildTableRows(model, sales, saleItems);
    }

    private async Task BuildChartData(ReportViewModel model, IEnumerable<CashFlow.Core.Entities.Sale> sales, IEnumerable<CashFlow.Core.Entities.SaleItem> saleItems)
    {
        // Revenue Chart - by period
        if (model.Period == "Daily")
        {
            var grouped = sales
                .GroupBy(s => s.SaleDate.ToString("dd MMM"))
                .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(s => s.NetAmount) })
                .OrderBy(g => g.Label)
                .ToList();
            model.RevenueChart = grouped;
        }
        else if (model.Period == "Weekly")
        {
            var grouped = sales
                .GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    s.SaleDate, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday))
                .Select(g => new ChartDataPoint
                {
                    Label = $"Week {g.Key}",
                    Value = g.Sum(s => s.NetAmount)
                })
                .OrderBy(g => g.Label)
                .ToList();
            model.RevenueChart = grouped;
        }
        else if (model.Period == "Monthly")
        {
            var grouped = sales
                .GroupBy(s => s.SaleDate.ToString("MMM yyyy"))
                .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(s => s.NetAmount) })
                .OrderBy(g => g.Label)
                .ToList();
            model.RevenueChart = grouped;
        }
        else // Yearly
        {
            var grouped = sales
                .GroupBy(s => s.SaleDate.ToString("yyyy"))
                .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(s => s.NetAmount) })
                .OrderBy(g => g.Label)
                .ToList();
            model.RevenueChart = grouped;
        }

        // Payment Chart
        model.PaymentChart = sales
            .GroupBy(s => s.PaymentMethod.ToString())
            .Select(g => new ChartDataPoint { Label = g.Key, Value = g.Sum(s => s.NetAmount) })
            .ToList();

        // Product Chart - Top 5 products
        var topProducts = saleItems
            .GroupBy(si => si.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.FirstOrDefault()?.Product?.ItemName ?? "Unknown",
                TotalRevenue = g.Sum(si => si.Subtotal)
            })
            .OrderByDescending(g => g.TotalRevenue)
            .Take(5)
            .ToList();

        model.ProductChart = topProducts
            .Select(g => new ChartDataPoint { Label = g.ProductName, Value = g.TotalRevenue })
            .ToList();
    }

    private async Task BuildTableRows(ReportViewModel model, IEnumerable<CashFlow.Core.Entities.Sale> sales, IEnumerable<CashFlow.Core.Entities.SaleItem> saleItems)
    {
        if (model.ReportType == "Sales")
        {
            model.Rows = sales.Select(s => new ReportRow
            {
                Date = s.SaleDate.ToString("dd MMM yyyy HH:mm"),
                InvoiceNumber = s.InvoiceNumber,
                Customer = s.CustomerId.HasValue ? $"{s.Customer?.FirstName} {s.Customer?.LastName}" : "Walk-in",
                Revenue = s.NetAmount,
                PaymentMethod = s.PaymentMethod.ToString()
            }).ToList();
        }
        else if (model.ReportType == "Products")
        {
            model.Rows = saleItems
                .GroupBy(si => si.ProductId)
                .Select(g => new ReportRow
                {
                    Product = g.FirstOrDefault()?.Product?.ItemName ?? "Unknown",
                    Quantity = g.Sum(si => si.Quantity),
                    Revenue = g.Sum(si => si.Subtotal),
                    Cost = g.Sum(si => si.CostPriceAtSale * si.Quantity),
                    Profit = g.Sum(si => si.Subtotal - (si.CostPriceAtSale * si.Quantity))
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();
        }
        else if (model.ReportType == "Customers")
        {
            model.Rows = sales
                .GroupBy(s => s.CustomerId)
                .Select(g => new ReportRow
                {
                    Customer = g.FirstOrDefault()?.CustomerId.HasValue == true ?
                        $"{g.First().Customer?.FirstName} {g.First().Customer?.LastName}" :
                        "Walk-in",
                    Revenue = g.Sum(s => s.NetAmount),
                    Quantity = g.Count()
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();
        }
        else // Profit
        {
            model.Rows = saleItems
                .GroupBy(si => si.ProductId)
                .Select(g => new ReportRow
                {
                    Product = g.FirstOrDefault()?.Product?.ItemName ?? "Unknown",
                    Quantity = g.Sum(si => si.Quantity),
                    Revenue = g.Sum(si => si.Subtotal),
                    Cost = g.Sum(si => si.CostPriceAtSale * si.Quantity),
                    Profit = g.Sum(si => si.Subtotal - (si.CostPriceAtSale * si.Quantity))
                })
                .OrderByDescending(r => r.Profit)
                .ToList();
        }
    }

    // GET: Reports/ExportPDF
    public async Task<IActionResult> ExportPDF(string reportType, string period, DateTime? dateFrom, DateTime? dateTo, string? paymentMethod)
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var model = new ReportViewModel
        {
            ReportType = reportType ?? "Sales",
            Period = period ?? "Monthly",
            DateFrom = dateFrom,
            DateTo = dateTo,
            PaymentMethod = paymentMethod
        };

        await LoadReportData(model, organizationId, storeId);

        // Build PDF using QuestPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Text($"CashFlow POS - {model.ReportType} Report")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Green.Darken2);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Summary
                        column.Item().Text("Summary").Bold().FontSize(14);
                        column.Item().PaddingBottom(10);
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Total Revenue: R {model.TotalRevenue:N2}");
                            row.RelativeItem().Text($"Total Orders: {model.TotalOrders}");
                            row.RelativeItem().Text($"Avg Order: R {model.AverageOrderValue:N2}");
                            row.RelativeItem().Text($"Total Profit: R {model.TotalProfit:N2}");
                        });

                        // Table
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                if (model.ReportType == "Sales")
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                }
                                else if (model.ReportType == "Products" || model.ReportType == "Profit")
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                }
                                else
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                }
                            });

                            // Headers
                            table.Header(header =>
                            {
                                header.Cell().Text("Date/Product").Bold();
                                header.Cell().Text("Invoice/Customer").Bold();
                                header.Cell().Text("Details").Bold();
                                header.Cell().Text("Amount").Bold();
                                header.Cell().Text("Payment/Profit").Bold();
                            });

                            // Data rows (limited to 20 rows for PDF)
                            var rows = model.Rows.Take(20).ToList();
                            foreach (var data in rows)
                            {
                                table.Cell().Text(data.Date ?? data.Product ?? "-");
                                table.Cell().Text(data.InvoiceNumber ?? data.Customer ?? "-");
                                table.Cell().Text(data.Customer ?? (data.Quantity?.ToString() ?? "-"));
                                table.Cell().Text(data.Revenue.HasValue ? $"R {data.Revenue:N2}" : "-");
                                table.Cell().Text(data.Profit.HasValue ? $"R {data.Profit:N2}" : (data.PaymentMethod ?? "-"));
                            }

                            if (model.Rows.Count > 20)
                            {
                                table.Cell().ColumnSpan(5).Text($"... and {model.Rows.Count - 20} more rows");
                            }
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text($"Generated on {DateTime.Now:dd MMM yyyy HH:mm}");
            });
        });

        var pdfBytes = document.GeneratePdf();

        // Log PDF export
        await LogAuditAsync(
            "Export",
            "Report",
            null,
            null,
            new { model.ReportType, model.Period, model.TotalRevenue, model.TotalOrders },
            $"Exported PDF report: {model.ReportType}. Revenue: {model.TotalRevenue:C}, Orders: {model.TotalOrders}",
            null);

        return File(pdfBytes, "application/pdf",
            $"Report_{model.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }
}