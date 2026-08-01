using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Web.Controllers;

public class StockCountController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public StockCountController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: StockCount
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, int page = 1)
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var stockCounts = await _unitOfWork.StockCounts
            .FindAsync(sc => sc.OrganizationId == organizationId);

        if (storeId.HasValue)
        {
            stockCounts = stockCounts.Where(sc => sc.StoreId == storeId.Value || sc.StoreId == null);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            stockCounts = stockCounts.Where(sc =>
                sc.CountNumber.ToLower().Contains(searchTerm) ||
                (sc.Notes != null && sc.Notes.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            stockCounts = stockCounts.Where(sc => sc.Status == statusFilter);
        }

        stockCounts = stockCounts.OrderByDescending(sc => sc.CreatedAt);

        var viewModels = new List<StockCountViewModel>();
        foreach (var sc in stockCounts)
        {
            var items = await _unitOfWork.StockCountItems
                .FindAsync(sci => sci.StockCountId == sc.Id);

            viewModels.Add(new StockCountViewModel
            {
                Id = sc.Id,
                CountNumber = sc.CountNumber,
                CountDate = sc.CountDate,
                Status = sc.Status,
                CreatedBy = sc.CreatedByUser?.FullName ?? "Unknown",
                CreatedAt = sc.CreatedAt,
                VerifiedBy = sc.VerifiedByUser?.FullName,
                VerifiedAt = sc.VerifiedAt,
                Notes = sc.Notes,
                TotalItems = items.Count(),
                ItemsWithDiscrepancy = items.Count(i => i.Difference != 0),
                TotalDiscrepancy = items.Sum(i => i.Difference),
                StoreName = sc.Store?.Name ?? "All Stores"
            });
        }

        int pageSize = 10;
        int totalCount = viewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paged = viewModels.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var model = new StockCountListViewModel
        {
            StockCounts = paged,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter
        };

        ViewBag.Statuses = new List<string> { "In Progress", "Completed", "Verified" };

        return View(model);
    }

    // GET: StockCount/Start
    public async Task<IActionResult> Start()
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Check if there's an existing open stock count
        var existingCount = await _unitOfWork.StockCounts
            .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId &&
                                      sc.Status == "In Progress" &&
                                      (sc.StoreId == storeId || sc.StoreId == null));

        if (existingCount != null)
        {
            return RedirectToAction("Count", new { id = existingCount.Id });
        }

        return View(new StartStockCountViewModel());
    }

    // POST: StockCount/Start
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartStockCountViewModel model)
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Check for existing open count
        var existingCount = await _unitOfWork.StockCounts
            .FirstOrDefaultAsync(sc => sc.OrganizationId == organizationId &&
                                      sc.Status == "In Progress" &&
                                      (sc.StoreId == storeId || sc.StoreId == null));

        if (existingCount != null)
        {
            return RedirectToAction("Count", new { id = existingCount.Id });
        }

        // Generate count number
        var countNumber = $"SC-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        // Get all active products
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);

        if (storeId.HasValue)
        {
            products = products.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
        }

        var stockCount = new StockCount
        {
            OrganizationId = organizationId,
            StoreId = storeId,
            CountNumber = countNumber,
            CountDate = DateTime.UtcNow,
            Status = "In Progress",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Notes = model.Notes
        };

        await _unitOfWork.StockCounts.AddAsync(stockCount);
        await _unitOfWork.SaveChangesAsync();

        // Create stock count items
        foreach (var product in products)
        {
            var item = new StockCountItem
            {
                StockCountId = stockCount.Id,
                ProductId = product.Id,
                SystemStock = product.CurrentStock,
                ActualStock = 0,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.StockCountItems.AddAsync(item);
        }

        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(
            "Create",
            "StockCount",
            stockCount.Id,
            null,
            stockCount,
            $"Started stock count: {countNumber} with {products.Count()} items",
            countNumber);

        return RedirectToAction("Count", new { id = stockCount.Id });
    }

    // GET: StockCount/Count/5
    public async Task<IActionResult> Count(int id)
    {
        var stockCount = await _unitOfWork.StockCounts.GetByIdAsync(id);
        if (stockCount == null)
        {
            return NotFound();
        }

        var items = await _unitOfWork.StockCountItems
            .FindAsync(sci => sci.StockCountId == id);

        var itemViewModels = new List<StockCountItemViewModel>();
        foreach (var item in items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            itemViewModels.Add(new StockCountItemViewModel
            {
                Id = item.Id,
                StockCountId = item.StockCountId,
                ProductId = item.ProductId,
                ItemCode = product?.ItemCode ?? "N/A",
                ItemName = product?.ItemName ?? "Unknown",
                Size = product?.Size,
                Category = product?.Category,
                SystemStock = item.SystemStock,
                ActualStock = item.ActualStock,
                Notes = item.Notes,
                Difference = item.Difference
            });
        }

        var summary = new StockCountSummaryViewModel
        {
            TotalItems = itemViewModels.Count,
            TotalCounted = itemViewModels.Count(i => i.ActualStock > 0),
            ItemsWithDiscrepancy = itemViewModels.Count(i => i.Difference != 0),
            TotalOver = itemViewModels.Where(i => i.Difference > 0).Sum(i => i.Difference),
            TotalShort = itemViewModels.Where(i => i.Difference < 0).Sum(i => Math.Abs(i.Difference)),
            PercentageComplete = itemViewModels.Count > 0 ?
                (decimal)itemViewModels.Count(i => i.ActualStock > 0) / itemViewModels.Count * 100 : 0
        };

        ViewBag.StockCount = stockCount;
        ViewBag.Summary = summary;

        return View(itemViewModels);
    }

    // POST: StockCount/UpdateItem
    [HttpPost]
    public async Task<IActionResult> UpdateItem(int itemId, int actualStock, string? notes)
    {
        var item = await _unitOfWork.StockCountItems.GetByIdAsync(itemId);
        if (item == null)
        {
            return Json(new { success = false, message = "Item not found" });
        }

        item.ActualStock = actualStock;
        item.Notes = notes ?? item.Notes;
        _unitOfWork.StockCountItems.Update(item);
        await _unitOfWork.SaveChangesAsync();

        return Json(new
        {
            success = true,
            difference = item.Difference,
            hasDiscrepancy = item.Difference != 0
        });
    }

    // POST: StockCount/Complete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var stockCount = await _unitOfWork.StockCounts.GetByIdAsync(id);
        if (stockCount == null)
        {
            return NotFound();
        }

        stockCount.Status = "Completed";
        _unitOfWork.StockCounts.Update(stockCount);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(
            "Complete",
            "StockCount",
            stockCount.Id,
            null,
            stockCount,
            $"Completed stock count: {stockCount.CountNumber}",
            stockCount.CountNumber);

        TempData["SuccessMessage"] = $"Stock count {stockCount.CountNumber} completed!";
        return RedirectToAction(nameof(Index));
    }

    // POST: StockCount/Verify/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var stockCount = await _unitOfWork.StockCounts.GetByIdAsync(id);
        if (stockCount == null)
        {
            return NotFound();
        }

        stockCount.Status = "Verified";
        stockCount.VerifiedBy = userId;
        stockCount.VerifiedAt = DateTime.UtcNow;
        _unitOfWork.StockCounts.Update(stockCount);
        await _unitOfWork.SaveChangesAsync();

        await LogAuditAsync(
            "Verify",
            "StockCount",
            stockCount.Id,
            null,
            stockCount,
            $"Verified stock count: {stockCount.CountNumber}",
            stockCount.CountNumber);

        TempData["SuccessMessage"] = $"Stock count {stockCount.CountNumber} verified!";
        return RedirectToAction(nameof(Index));
    }

    // GET: StockCount/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var stockCount = await _unitOfWork.StockCounts.GetByIdAsync(id);
        if (stockCount == null)
        {
            return NotFound();
        }

        var items = await _unitOfWork.StockCountItems
            .FindAsync(sci => sci.StockCountId == id);

        var itemViewModels = new List<StockCountItemViewModel>();
        foreach (var item in items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            itemViewModels.Add(new StockCountItemViewModel
            {
                Id = item.Id,
                StockCountId = item.StockCountId,
                ProductId = item.ProductId,
                ItemCode = product?.ItemCode ?? "N/A",
                ItemName = product?.ItemName ?? "Unknown",
                Size = product?.Size,
                Category = product?.Category,
                SystemStock = item.SystemStock,
                ActualStock = item.ActualStock,
                Notes = item.Notes,
                Difference = item.Difference
            });
        }

        var summary = new StockCountSummaryViewModel
        {
            TotalItems = itemViewModels.Count,
            TotalCounted = itemViewModels.Count(i => i.ActualStock > 0),
            ItemsWithDiscrepancy = itemViewModels.Count(i => i.Difference != 0),
            TotalOver = itemViewModels.Where(i => i.Difference > 0).Sum(i => i.Difference),
            TotalShort = itemViewModels.Where(i => i.Difference < 0).Sum(i => Math.Abs(i.Difference)),
            PercentageComplete = itemViewModels.Count > 0 ?
                (decimal)itemViewModels.Count(i => i.ActualStock > 0) / itemViewModels.Count * 100 : 0
        };

        ViewBag.StockCount = stockCount;
        ViewBag.Summary = summary;

        return View(itemViewModels);
    }
}