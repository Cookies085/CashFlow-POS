using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CashFlow.Web.Controllers;

public class ProductsController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductsController(IUnitOfWork unitOfWork, IAuditService auditService) : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Products
    public async Task<IActionResult> Index(string? searchTerm, string? category, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get all products for the organization
        var products = await _unitOfWork.Products.FindAsync(p =>
            p.OrganizationId == organizationId);

        // Filter by store if user is in a specific store
        if (storeId.HasValue)
        {
            products = products.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            products = products.Where(p =>
                p.ItemCode.ToLower().Contains(searchTerm) ||
                p.ItemName.ToLower().Contains(searchTerm) ||
                p.Barcode != null && p.Barcode.ToLower().Contains(searchTerm));
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
            products = products.Where(p => p.Category == category);
        }

        // Get unique categories for filter dropdown
        var categories = (await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && !string.IsNullOrEmpty(p.Category)))
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        // Convert to ViewModel
        var productViewModels = products.Select(p => new ProductViewModel
        {
            Id = p.Id,
            ItemCode = p.ItemCode,
            ItemName = p.ItemName,
            Size = p.Size,
            Category = p.Category,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            PricingMode = p.PricingMode,
            MarkupPercentage = p.MarkupPercentage,
            CurrentStock = p.CurrentStock,
            MinStockAlert = p.MinStockAlert,
            ReorderPoint = p.ReorderPoint,
            SupplierId = p.SupplierId,
            Barcode = p.Barcode,
            Weight = p.Weight,
            Unit = p.Unit,
            TaxRate = p.TaxRate,
            IsActive = p.IsActive
        }).ToList();

        // Pagination
        int pageSize = 10;
        int totalCount = productViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedProducts = productViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new ProductListViewModel
        {
            Products = pagedProducts,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Category = category
        };

        ViewBag.Categories = categories;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentCategory = category;

        // Log view products action
        await LogAuditAsync(
            "View",
            "Products",
            null,
            null,
            null,
            $"Viewed products list. Page: {page}, Count: {totalCount}",
            null);

        return View(model);
    }

    // GET: Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var model = new ProductViewModel
        {
            Id = product.Id,
            ItemCode = product.ItemCode,
            ItemName = product.ItemName,
            Size = product.Size,
            Category = product.Category,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            PricingMode = product.PricingMode,
            MarkupPercentage = product.MarkupPercentage,
            CurrentStock = product.CurrentStock,
            MinStockAlert = product.MinStockAlert,
            ReorderPoint = product.ReorderPoint,
            SupplierId = product.SupplierId,
            Barcode = product.Barcode,
            Weight = product.Weight,
            Unit = product.Unit,
            TaxRate = product.TaxRate,
            IsActive = product.IsActive
        };

        await LogAuditAsync(
            "View",
            "Product",
            product.Id,
            null,
            null,
            $"Viewed product details: {product.ItemName} ({product.ItemCode})",
            product.ItemCode);

        return View(model);
    }

    // GET: Products/Create
    public async Task<IActionResult> Create()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get suppliers for dropdown
        var suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        ViewBag.Suppliers = suppliers;
        ViewBag.StoreId = HttpContext.Session.GetInt32("StoreId");

        return View(new ProductViewModel());
    }

    // POST: Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (ModelState.IsValid)
        {
            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var storeId = HttpContext.Session.GetInt32("StoreId");

            // Check if item code already exists
            var existingProduct = await _unitOfWork.Products
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.ItemCode == model.ItemCode);

            if (existingProduct != null)
            {
                ModelState.AddModelError("ItemCode", "Item code already exists.");
                ViewBag.Suppliers = await _unitOfWork.Suppliers
                    .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);
                return View(model);
            }

            // Calculate selling price if using markup
            if (model.PricingMode == "markup" && model.MarkupPercentage.HasValue)
            {
                model.SellingPrice = model.CostPrice * (1 + (model.MarkupPercentage.Value / 100));
            }

            var product = new Product
            {
                OrganizationId = organizationId,
                ItemCode = model.ItemCode,
                ItemName = model.ItemName,
                Size = model.Size,
                Category = model.Category,
                CostPrice = model.CostPrice,
                SellingPrice = model.SellingPrice,
                PricingMode = model.PricingMode,
                MarkupPercentage = model.MarkupPercentage,
                CurrentStock = model.CurrentStock,
                MinStockAlert = model.MinStockAlert,
                ReorderPoint = model.ReorderPoint,
                SupplierId = model.SupplierId,
                Barcode = model.Barcode,
                Weight = model.Weight,
                Unit = model.Unit,
                TaxRate = model.TaxRate,
                IsActive = model.IsActive,
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            await LogAuditAsync(
                "Create",
                "Product",
                product.Id,
                null,
                product,
                $"Created product: {product.ItemName} ({product.ItemCode})",
                product.ItemCode);

            TempData["SuccessMessage"] = "Product created successfully!";
            return RedirectToAction(nameof(Index));
        }

        var orgId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        ViewBag.Suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == orgId && s.IsActive);
        ViewBag.StoreId = HttpContext.Session.GetInt32("StoreId");

        return View(model);
    }

    // GET: Products/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        var suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);
        ViewBag.Suppliers = suppliers;

        var model = new ProductViewModel
        {
            Id = product.Id,
            ItemCode = product.ItemCode,
            ItemName = product.ItemName,
            Size = product.Size,
            Category = product.Category,
            CostPrice = product.CostPrice,
            SellingPrice = product.SellingPrice,
            PricingMode = product.PricingMode,
            MarkupPercentage = product.MarkupPercentage,
            CurrentStock = product.CurrentStock,
            MinStockAlert = product.MinStockAlert,
            ReorderPoint = product.ReorderPoint,
            SupplierId = product.SupplierId,
            Barcode = product.Barcode,
            Weight = product.Weight,
            Unit = product.Unit,
            TaxRate = product.TaxRate,
            IsActive = product.IsActive
        };

        return View(model);
    }

    // POST: Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var product = await _unitOfWork.Products.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                // Check if item code already exists (excluding current product)
                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var existingProduct = await _unitOfWork.Products
                    .FirstOrDefaultAsync(p => p.OrganizationId == organizationId &&
                                             p.ItemCode == model.ItemCode &&
                                             p.Id != id);

                if (existingProduct != null)
                {
                    ModelState.AddModelError("ItemCode", "Item code already exists.");
                    var suppliers = await _unitOfWork.Suppliers
                        .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);
                    ViewBag.Suppliers = suppliers;
                    return View(model);
                }

                // Calculate selling price if using markup
                if (model.PricingMode == "markup" && model.MarkupPercentage.HasValue)
                {
                    model.SellingPrice = model.CostPrice * (1 + (model.MarkupPercentage.Value / 100));
                }

                // Save old values for audit
                var oldProduct = new
                {
                    product.ItemCode,
                    product.ItemName,
                    product.Size,
                    product.Category,
                    product.CostPrice,
                    product.SellingPrice,
                    product.PricingMode,
                    product.MarkupPercentage,
                    product.CurrentStock,
                    product.MinStockAlert,
                    product.ReorderPoint,
                    product.SupplierId,
                    product.Barcode,
                    product.Weight,
                    product.Unit,
                    product.TaxRate,
                    product.IsActive
                };

                product.ItemCode = model.ItemCode;
                product.ItemName = model.ItemName;
                product.Size = model.Size;
                product.Category = model.Category;
                product.CostPrice = model.CostPrice;
                product.SellingPrice = model.SellingPrice;
                product.PricingMode = model.PricingMode;
                product.MarkupPercentage = model.MarkupPercentage;
                product.CurrentStock = model.CurrentStock;
                product.MinStockAlert = model.MinStockAlert;
                product.ReorderPoint = model.ReorderPoint;
                product.SupplierId = model.SupplierId;
                product.Barcode = model.Barcode;
                product.Weight = model.Weight;
                product.Unit = model.Unit;
                product.TaxRate = model.TaxRate;
                product.IsActive = model.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();

                await LogAuditAsync(
                    "Edit",
                    "Product",
                    product.Id,
                    oldProduct,
                    product,
                    $"Updated product: {product.ItemName} ({product.ItemCode})",
                    product.ItemCode);

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while updating the product.");
            }
        }

        var orgId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        ViewBag.Suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == orgId && s.IsActive);

        return View(model);
    }

    // GET: Products/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var model = new ProductViewModel
        {
            Id = product.Id,
            ItemCode = product.ItemCode,
            ItemName = product.ItemName,
            Category = product.Category,
            CurrentStock = product.CurrentStock,
            IsActive = product.IsActive
        };

        return View(model);
    }

    // POST: Products/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product != null)
        {
            // Save old values for audit
            var oldProduct = new
            {
                product.ItemCode,
                product.ItemName,
                product.Category,
                product.CurrentStock,
                product.IsActive
            };

            // Soft delete - just deactivate
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            await LogAuditAsync(
                "Delete",
                "Product",
                product.Id,
                oldProduct,
                null,
                $"Deleted product: {product.ItemName} ({product.ItemCode})",
                product.ItemCode);

            TempData["SuccessMessage"] = "Product deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Products/GetLowStock
    public async Task<IActionResult> GetLowStock()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        var lowStockProducts = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId &&
                           p.IsActive &&
                           p.CurrentStock <= p.MinStockAlert);

        var model = lowStockProducts.Select(p => new ProductViewModel
        {
            Id = p.Id,
            ItemCode = p.ItemCode,
            ItemName = p.ItemName,
            CurrentStock = p.CurrentStock,
            MinStockAlert = p.MinStockAlert,
            ReorderPoint = p.ReorderPoint,
        }).ToList();

        await LogAuditAsync(
            "View",
            "LowStock",
            null,
            null,
            null,
            $"Viewed low stock report. Found {model.Count} items below min stock",
            null);

        return View(model);
    }

    // GET: Products/ExpiryAlerts
    // GET: Products/ExpiryAlerts
    public async Task<IActionResult> ExpiryAlerts(string? filter = null)
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get all stock movements with expiry dates and positive stock
        var movements = await _unitOfWork.StockMovements
            .FindAsync(sm => sm.ExpiryDate.HasValue && sm.Quantity > 0);

        if (storeId.HasValue)
        {
            movements = movements.Where(sm => sm.StoreId == storeId.Value || sm.StoreId == null);
        }

        var alerts = new List<ExpiryAlertViewModel>();

        foreach (var movement in movements)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(movement.ProductId);
            if (product == null || !product.IsActive) continue;

            var daysUntilExpiry = (movement.ExpiryDate.Value.Date - DateTime.UtcNow.Date).Days;

            alerts.Add(new ExpiryAlertViewModel
            {
                ProductId = product.Id,
                ItemCode = product.ItemCode,
                ItemName = product.ItemName,
                BatchNumber = movement.BatchNumber ?? "N/A",
                Quantity = movement.Quantity,
                ExpiryDate = movement.ExpiryDate.Value,
                DaysUntilExpiry = daysUntilExpiry,
                Status = daysUntilExpiry < 0 ? "Expired" :
                         daysUntilExpiry <= 3 ? "Urgent" :
                         daysUntilExpiry <= 7 ? "Warning" :
                         daysUntilExpiry <= 30 ? "Notice" : "OK"
            });
        }

        // Calculate totals before filtering
        ViewBag.TotalExpired = alerts.Count(a => a.DaysUntilExpiry < 0);
        ViewBag.TotalExpiring = alerts.Count(a => a.DaysUntilExpiry >= 0 && a.DaysUntilExpiry <= 30);

        // Apply filter
        if (filter == "expired")
        {
            alerts = alerts.Where(a => a.DaysUntilExpiry < 0).ToList();
        }
        else if (filter == "expiring")
        {
            alerts = alerts.Where(a => a.DaysUntilExpiry >= 0 && a.DaysUntilExpiry <= 30).ToList();
        }
        // else show all

        ViewBag.ActiveFilter = filter;

        alerts = alerts.OrderBy(a => a.DaysUntilExpiry).ToList();

        return View(alerts);
    }
}