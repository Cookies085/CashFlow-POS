using CashFlow.Core.Interfaces;
using CashFlow.Core.Enums;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Web.Controllers;

public class SalesController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public SalesController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Sales
    public async Task<IActionResult> Index(string? searchTerm, string? dateFrom, string? dateTo, string? paymentMethod, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get all sales for the organization
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.OrganizationId == organizationId);

        // Filter by store
        if (storeId.HasValue)
        {
            sales = sales.Where(s => s.StoreId == storeId.Value);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            sales = sales.Where(s =>
                s.InvoiceNumber.ToLower().Contains(searchTerm) ||
                (s.Customer != null && s.Customer.FirstName.ToLower().Contains(searchTerm)) ||
                (s.Customer != null && s.Customer.LastName.ToLower().Contains(searchTerm)) ||
                (s.Customer != null && s.Customer.Phone.Contains(searchTerm)));
        }

        // Apply date filters
        if (!string.IsNullOrEmpty(dateFrom))
        {
            var fromDate = DateTime.Parse(dateFrom);
            sales = sales.Where(s => s.SaleDate >= fromDate);
        }

        if (!string.IsNullOrEmpty(dateTo))
        {
            var toDate = DateTime.Parse(dateTo).AddDays(1);
            sales = sales.Where(s => s.SaleDate <= toDate);
        }

        // Apply payment method filter
        if (!string.IsNullOrEmpty(paymentMethod))
        {
            sales = sales.Where(s => s.PaymentMethod.ToString() == paymentMethod);
        }

        // Order by date descending
        sales = sales.OrderByDescending(s => s.SaleDate);

        // Get totals
        var totalSales = sales.Sum(s => s.NetAmount);
        var totalOrders = sales.Count();

        // Convert to ViewModel
        var saleViewModels = sales.Select(s => new SaleViewModel
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            CustomerId = s.CustomerId,
            CustomerName = s.CustomerId.HasValue ?
                $"{s.Customer?.FirstName} {s.Customer?.LastName}" :
                "Walk-in Customer",
            SaleDate = s.SaleDate,
            TotalAmount = s.TotalAmount,
            TaxAmount = s.TaxAmount,
            DiscountAmount = s.DiscountAmount,
            NetAmount = s.NetAmount,
            PaymentMethod = s.PaymentMethod.ToString(),
            PaymentStatus = s.PaymentStatus,
            CashierName = s.User?.FullName ?? "Unknown",
            ItemCount = s.SaleItems?.Count ?? 0
        }).ToList();

        // Pagination
        int pageSize = 10;
        int totalCount = saleViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedSales = saleViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new SalesListViewModel
        {
            Sales = pagedSales,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PaymentMethod = paymentMethod,
            TotalSales = totalSales,
            TotalOrders = totalOrders
        };

        // Get payment methods for filter dropdown
        var paymentMethods = Enum.GetValues(typeof(PaymentMethod))
            .Cast<PaymentMethod>()
            .Select(p => p.ToString())
            .ToList();

        ViewBag.PaymentMethods = paymentMethods;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;
        ViewBag.CurrentPaymentMethod = paymentMethod;

        // Log view sales
        await LogAuditAsync(
            "View",
            "Sales",
            null,
            null,
            null,
            $"Viewed sales list. Page: {page}, Total sales: {totalCount}, Revenue: {totalSales:C}",
            null);

        return View(model);
    }

    // GET: Sales/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var sale = await _unitOfWork.Sales.GetByIdAsync(id);
        if (sale == null)
        {
            return NotFound();
        }

        // Get sale items
        var saleItems = await _unitOfWork.SaleItems
            .FindAsync(si => si.SaleId == id);

        var model = new SaleDetailsViewModel
        {
            Id = sale.Id,
            InvoiceNumber = sale.InvoiceNumber,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerId.HasValue ?
                $"{sale.Customer?.FirstName} {sale.Customer?.LastName}" :
                "Walk-in Customer",
            CustomerPhone = sale.Customer?.Phone ?? "",
            CustomerEmail = sale.Customer?.Email ?? "",
            SaleDate = sale.SaleDate,
            TotalAmount = sale.TotalAmount,
            TaxAmount = sale.TaxAmount,
            DiscountAmount = sale.DiscountAmount,
            NetAmount = sale.NetAmount,
            PaymentMethod = sale.PaymentMethod.ToString(),
            PaymentStatus = sale.PaymentStatus,
            CashierName = sale.User?.FullName ?? "Unknown",
            Notes = sale.Notes,
            Items = saleItems.Select(si => new SaleItemViewModel
            {
                ProductId = si.ProductId,
                ItemName = si.Product?.ItemName ?? "Unknown Product",
                ItemCode = si.Product?.ItemCode ?? "N/A",
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                Discount = si.Discount,
                TaxAmount = si.TaxAmount,
                Subtotal = si.Subtotal
            }).ToList()
        };

        // Log view sale details
        await LogAuditAsync(
            "View",
            "Sale",
            sale.Id,
            null,
            sale,
            $"Viewed sale details: {sale.InvoiceNumber}, Amount: {sale.NetAmount:C}",
            sale.InvoiceNumber);

        return View(model);
    }

    // GET: Sales/Receipt/5
    public async Task<IActionResult> Receipt(int id)
    {
        var sale = await _unitOfWork.Sales.GetByIdAsync(id);
        if (sale == null)
        {
            return NotFound();
        }

        // Get sale items
        var saleItems = await _unitOfWork.SaleItems
            .FindAsync(si => si.SaleId == id);

        ViewBag.Sale = sale;
        ViewBag.Items = saleItems;
        ViewBag.StoreName = HttpContext.Session.GetString("StoreName") ?? "Main Store";

        // Log receipt view
        await LogAuditAsync(
            "View",
            "Receipt",
            sale.Id,
            null,
            sale,
            $"Viewed receipt for sale: {sale.InvoiceNumber}",
            sale.InvoiceNumber);

        return View();
    }

    // POST: Sales/Void/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id)
    {
        var sale = await _unitOfWork.Sales.GetByIdAsync(id);
        if (sale == null)
        {
            return Json(new { success = false, message = "Sale not found" });
        }

        // Only allow voiding if not already voided
        if (sale.PaymentStatus == "Voided")
        {
            return Json(new { success = false, message = "Sale is already voided" });
        }

        // Save old values for audit
        var oldSale = new
        {
            sale.InvoiceNumber,
            sale.NetAmount,
            sale.PaymentStatus,
            sale.CustomerId
        };

        // Update sale status
        sale.PaymentStatus = "Voided";
        _unitOfWork.Sales.Update(sale);

        // Restore stock
        var saleItems = await _unitOfWork.SaleItems
            .FindAsync(si => si.SaleId == id);

        var restoredItems = new List<string>();
        foreach (var item in saleItems)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.CurrentStock += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);
                restoredItems.Add($"{item.Quantity} x {product.ItemName}");
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Log audit
        await LogAuditAsync(
            "Void",
            "Sale",
            sale.Id,
            oldSale,
            sale,
            $"Voided sale: {sale.InvoiceNumber}. Amount: {sale.NetAmount:C}. Restored stock: {string.Join(", ", restoredItems)}",
            sale.InvoiceNumber);

        TempData["SuccessMessage"] = $"Sale {sale.InvoiceNumber} has been voided successfully.";
        return RedirectToAction(nameof(Details), new { id = id });
    }

    // GET: Sales/PrintInvoice/5
    public async Task<IActionResult> PrintInvoice(int id)
    {
        var sale = await _unitOfWork.Sales.GetByIdAsync(id);
        if (sale == null)
        {
            return NotFound();
        }

        var saleItems = await _unitOfWork.SaleItems
            .FindAsync(si => si.SaleId == id);

        ViewBag.Sale = sale;
        ViewBag.Items = saleItems;
        ViewBag.StoreName = HttpContext.Session.GetString("StoreName") ?? "Main Store";

        // Log print invoice
        await LogAuditAsync(
            "Print",
            "Invoice",
            sale.Id,
            null,
            sale,
            $"Printed invoice for sale: {sale.InvoiceNumber}",
            sale.InvoiceNumber);

        return View();
    }
}