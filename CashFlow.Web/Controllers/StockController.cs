using System.ComponentModel.DataAnnotations;
using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class StockController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public StockController(IUnitOfWork unitOfWork, IAuditService auditService, INotificationService notificationService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    // GET: Stock
    public async Task<IActionResult> Index(string? searchTerm, string? movementType, string? dateFrom, string? dateTo, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get all stock movements with Product included
        var allMovements = await _unitOfWork.GetStockMovementsWithProductAsync(organizationId);

        // Filter by store
        if (storeId.HasValue)
        {
            allMovements = allMovements.Where(sm => sm.StoreId == storeId.Value);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            allMovements = allMovements.Where(sm =>
                (sm.Product != null && sm.Product.ItemCode != null && sm.Product.ItemCode.ToLower().Contains(searchTerm)) ||
                (sm.Product != null && sm.Product.ItemName != null && sm.Product.ItemName.ToLower().Contains(searchTerm)) ||
                (sm.Reference != null && sm.Reference.ToLower().Contains(searchTerm)) ||
                (sm.BatchNumber != null && sm.BatchNumber.ToLower().Contains(searchTerm)));
        }

        // Apply movement type filter
        if (!string.IsNullOrEmpty(movementType) && Enum.TryParse<MovementType>(movementType, out var type))
        {
            allMovements = allMovements.Where(sm => sm.MovementType == type);
        }

        // Apply date filters
        if (!string.IsNullOrEmpty(dateFrom))
        {
            var fromDate = DateTime.Parse(dateFrom);
            allMovements = allMovements.Where(sm => sm.CreatedAt >= fromDate);
        }

        if (!string.IsNullOrEmpty(dateTo))
        {
            var toDate = DateTime.Parse(dateTo).AddDays(1);
            allMovements = allMovements.Where(sm => sm.CreatedAt <= toDate);
        }

        // Order by date descending
        allMovements = allMovements.OrderByDescending(sm => sm.CreatedAt);

        // Convert to ViewModel - now Product data is available
        var movementViewModels = allMovements.Select(sm => new StockMovementViewModel
        {
            Id = sm.Id,
            ProductId = sm.ProductId,
            ProductName = sm.Product != null ? sm.Product.ItemName : "Unknown Product",
            ProductCode = sm.Product != null ? sm.Product.ItemCode : "N/A",
            MovementType = sm.MovementType,
            Quantity = sm.Quantity,
            UnitCost = sm.UnitCost,
            UnitPrice = sm.UnitPrice,
            ExpiryDate = sm.ExpiryDate,
            BatchNumber = sm.BatchNumber,
            Reference = sm.Reference,
            Notes = sm.Notes,
            StoreId = sm.StoreId,
            CreatedBy = sm.CreatedBy.HasValue ? "User" : "System",
            CreatedAt = sm.CreatedAt
        }).ToList();

        // Pagination
        int pageSize = 10;
        int totalCount = movementViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedMovements = movementViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new StockMovementListViewModel
        {
            Movements = pagedMovements,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            MovementType = movementType,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        // Get movement types for filter
        var movementTypes = Enum.GetValues(typeof(MovementType))
            .Cast<MovementType>()
            .Select(m => m.ToString())
            .ToList();

        ViewBag.MovementTypes = movementTypes;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentMovementType = movementType;
        ViewBag.DateFrom = dateFrom;
        ViewBag.DateTo = dateTo;

        // Log view stock movements
        await LogAuditAsync(
            "View",
            "StockMovements",
            null,
            null,
            null,
            $"Viewed stock movements. Page: {page}, Total: {totalCount}, Filter: {(string.IsNullOrEmpty(searchTerm) ? "None" : searchTerm)}",
            null);

        return View(model);
    }

    // GET: Stock/Receive
    public async Task<IActionResult> Receive()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get products for dropdown
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);

        // Get suppliers for dropdown
        var suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        // Get cash accounts
        var cashAccounts = await _unitOfWork.CashAccounts
            .FindAsync(ca => ca.OrganizationId == organizationId && (ca.StoreId == storeId || ca.StoreId == null) && ca.IsActive);

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();
        ViewBag.Suppliers = suppliers.OrderBy(s => s.Name).ToList();
        ViewBag.CashAccounts = cashAccounts.OrderBy(ca => ca.AccountName).ToList();

        // Default source types
        ViewBag.SourceTypes = new List<string> { "Purchase", "Transfer", "Return", "Adjustment", "Donation", "Consignment" };

        return View(new StockReceiveViewModel());
    }

    // GET: Stock/SearchProductsForReceive
    [HttpGet]
    public async Task<IActionResult> SearchProductsForReceive(string term)
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        if (string.IsNullOrEmpty(term) || term.Length < 2)
        {
            return Json(new List<object>());
        }

        term = term.ToLower();
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId &&
                           p.IsActive &&
                           (p.StoreId == storeId || p.StoreId == null) &&
                           (p.ItemCode.ToLower().Contains(term) ||
                            p.ItemName.ToLower().Contains(term) ||
                            (p.Barcode != null && p.Barcode.ToLower().Contains(term))));

        var result = products
            .OrderBy(p => p.ItemName)
            .Select(p => new
            {
                id = p.Id,
                itemCode = p.ItemCode,
                itemName = p.ItemName,
                size = p.Size,
                sellingPrice = p.SellingPrice,
                costPrice = p.CostPrice,
                currentStock = p.CurrentStock,
                barcode = p.Barcode,
                taxRate = p.TaxRate
            })
            .Take(20)
            .ToList();

        return Json(result);
    }

    // POST: Stock/Receive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(StockReceiveViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var storeId = HttpContext.Session.GetInt32("StoreId");

                // Get the product
                var product = await _unitOfWork.Products.GetByIdAsync(model.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("ProductId", "Product not found.");
                    await PopulateDropdowns();
                    return View(model);
                }

                // Check if product has expiry date tracking
                // (This is optional; you can add a flag on product if needed)

                // If unit price is not provided, use the product's selling price
                var unitPrice = model.UnitPrice ?? product.SellingPrice;

                // Save previous values for audit/notification
                var previousCost = product.CostPrice;
                var previousPrice = product.SellingPrice;

                // Update product stock
                product.CurrentStock += model.Quantity;
                product.CostPrice = model.UnitCost; // Update cost price
                if (model.UnitPrice.HasValue)
                    product.SellingPrice = model.UnitPrice.Value;
                product.UpdatedAt = DateTime.UtcNow;

                // If supplier is provided, update product supplier
                if (model.SupplierId.HasValue)
                {
                    product.SupplierId = model.SupplierId.Value;
                }

                _unitOfWork.Products.Update(product);

                // Detect cost increase
                var costIncreased = previousCost > 0 && model.UnitCost > previousCost;
                var costDifference = costIncreased ? model.UnitCost - previousCost : 0;
                var costIncreasePercentage = previousCost > 0 ? (costDifference / previousCost) * 100 : 0;

                // Create stock movement
                var movement = new StockMovement
                {
                    ProductId = model.ProductId,
                    MovementDate = DateTime.UtcNow,
                    MovementType = MovementType.Purchase,
                    Quantity = model.Quantity,
                    UnitCost = model.UnitCost,
                    UnitPrice = unitPrice,
                    ExpiryDate = model.ExpiryDate,
                    BatchNumber = model.BatchNumber,
                    Reference = model.Reference ?? $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    Notes = model.Notes,
                    StoreId = storeId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    // New fields
                    SourceType = model.SourceType,
                    ReceiptNumber = model.ReceiptNumber,
                    PreviousUnitCost = previousCost,
                    PreviousSellingPrice = previousPrice,
                    CostIncreased = costIncreased,
                    IsValidated = false // Will be validated after receipt upload/verification
                };

                // Handle receipt upload
                if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "receipts");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(model.ReceiptFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ReceiptFile.CopyToAsync(stream);
                    }

                    movement.ReceiptImageUrl = $"/uploads/receipts/{uniqueFileName}";
                }

                // Receipt validation (if both quantity and total provided)
                if (model.ReceiptQuantity.HasValue && model.ReceiptTotal.HasValue)
                {
                    var qtyMatch = model.Quantity == model.ReceiptQuantity.Value;
                    var amountMatch = Math.Abs(model.TotalCost - model.ReceiptTotal.Value) < 0.01m;
                    movement.IsValidated = qtyMatch && amountMatch;
                    movement.ValidationNotes = qtyMatch && amountMatch ?
                        "Receipt validated successfully." :
                        $"Validation failed: {(qtyMatch ? "" : "Quantity mismatch. ")}{(amountMatch ? "" : "Amount mismatch.")}";
                    movement.ValidatedBy = userId;
                    movement.ValidatedAt = DateTime.UtcNow;
                }

                await _unitOfWork.StockMovements.AddAsync(movement);
                await _unitOfWork.SaveChangesAsync();

                // ----- Handle Cash Deduction -----
                // Only deduct if SourceType is "Purchase" and cash account is selected
                if (model.SourceType == "Purchase" && model.CashAccountId.HasValue)
                {
                    var cashAccount = await _unitOfWork.CashAccounts.GetByIdAsync(model.CashAccountId.Value);
                    if (cashAccount != null && cashAccount.Balance >= model.TotalCost)
                    {
                        // Deduct from cash account
                        cashAccount.Balance -= model.TotalCost;
                        cashAccount.UpdatedAt = DateTime.UtcNow;
                        cashAccount.UpdatedBy = userId;
                        _unitOfWork.CashAccounts.Update(cashAccount);

                        // Record transaction
                        var cashTransaction = new CashTransaction
                        {
                            OrganizationId = organizationId,
                            StoreId = storeId,
                            CashAccountId = cashAccount.Id,
                            TransactionType = "Debit",
                            Amount = model.TotalCost,
                            Reference = movement.Reference,
                            Description = $"Stock purchase: {product.ItemName} x {model.Quantity}",
                            TransactionDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = userId
                        };
                        await _unitOfWork.CashTransactions.AddAsync(cashTransaction);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    else
                    {
                        // Insufficient balance or account not found - log warning but proceed
                        // Optionally add a notification here
                        TempData["WarningMessage"] = "Insufficient cash balance. Stock received but cash not deducted.";
                    }
                }

                // ----- Send Notifications -----
                // 1. Cost increase notification
                if (costIncreased)
                {
                    await _notificationService.SendNotificationAsync(
                        organizationId: organizationId,
                        userId: null, // all users
                        storeId: storeId,
                        title: "⚠️ Cost Increased",
                        message: $"Cost for {product.ItemName} increased from R{previousCost:N2} to R{model.UnitCost:N2} (+{costIncreasePercentage:N1}%). Please review selling price.",
                        type: "Warning",
                        link: $"/Products/Edit/{product.Id}",
                        reference: $"cost-increase-{product.Id}-{DateTime.Now:yyyyMMdd}"
                    );
                }

                // 2. Receipt validation notification
                if (model.ReceiptQuantity.HasValue && model.ReceiptTotal.HasValue)
                {
                    if (!movement.IsValidated)
                    {
                        await _notificationService.SendNotificationAsync(
                            organizationId: organizationId,
                            userId: null,
                            storeId: storeId,
                            title: "⚠️ Receipt Validation Failed",
                            message: $"Receipt for {product.ItemName} (PO: {movement.Reference}) validation failed. Please check quantities and amounts.",
                            type: "Danger",
                            link: $"/Stock/Details/{movement.Id}",
                            reference: $"receipt-fail-{movement.Id}"
                        );
                    }
                    else
                    {
                        await _notificationService.SendNotificationAsync(
                            organizationId: organizationId,
                            userId: null,
                            storeId: storeId,
                            title: "✅ Receipt Validated",
                            message: $"Receipt for {product.ItemName} (PO: {movement.Reference}) validated successfully.",
                            type: "Success",
                            link: $"/Stock/Details/{movement.Id}",
                            reference: $"receipt-ok-{movement.Id}"
                        );
                    }
                }

                // 3. Low cash balance notification
                if (model.CashAccountId.HasValue)
                {
                    var cashAccount = await _unitOfWork.CashAccounts.GetByIdAsync(model.CashAccountId.Value);
                    if (cashAccount != null && cashAccount.Balance < 500)
                    {
                        await _notificationService.SendNotificationAsync(
                            organizationId: organizationId,
                            userId: null,
                            storeId: storeId,
                            title: "💰 Low Cash Balance",
                            message: $"Cash balance for {cashAccount.AccountName} is low: R{cashAccount.Balance:N2}. Please top up.",
                            type: "Warning",
                            link: "/Settings",
                            reference: $"low-cash-{cashAccount.Id}"
                        );
                    }
                }

                TempData["SuccessMessage"] = $"Stock received successfully for {product.ItemName}! " +
                    (costIncreased ? $"Cost increased from R{previousCost:N2} to R{model.UnitCost:N2}." : "") +
                    (movement.IsValidated ? " Receipt validated." : " Receipt pending validation.");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Stock/Adjust
    public async Task<IActionResult> Adjust()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get products for dropdown
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();

        return View(new StockMovementViewModel());
    }

    // POST: Stock/Adjust
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(StockMovementViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var storeId = HttpContext.Session.GetInt32("StoreId");

                // Get the product
                var product = await _unitOfWork.Products.GetByIdAsync(model.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("ProductId", "Product not found.");
                    await PopulateProductDropdown();
                    return View(model);
                }

                // Save old values for audit
                var oldStock = product.CurrentStock;
                var oldValues = new { product.CurrentStock, product.UpdatedAt };

                // For adjustments, quantity is always positive
                // The movement type determines if it's addition or subtraction
                var isAddition = model.MovementType == MovementType.Purchase ||
                                model.MovementType == MovementType.Correction;

                // Update product stock
                if (isAddition)
                {
                    product.CurrentStock += model.Quantity;
                }
                else
                {
                    // Check if sufficient stock
                    if (product.CurrentStock < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Current stock: {product.CurrentStock}");
                        await PopulateProductDropdown();
                        return View(model);
                    }
                    product.CurrentStock -= model.Quantity;
                }

                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);

                // Create stock movement
                var movement = new StockMovement
                {
                    ProductId = model.ProductId,
                    MovementDate = DateTime.UtcNow,
                    MovementType = model.MovementType,
                    Quantity = model.Quantity,
                    UnitCost = product.CostPrice,
                    UnitPrice = product.SellingPrice,
                    ExpiryDate = model.ExpiryDate,
                    BatchNumber = model.BatchNumber,
                    Reference = model.Reference ?? $"{model.MovementType}-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    Notes = model.Notes,
                    StoreId = storeId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.StockMovements.AddAsync(movement);
                await _unitOfWork.SaveChangesAsync();

                var action = isAddition ? "added to" : "removed from";

                // Log audit
                await LogAuditAsync(
                    "Adjust",
                    "Stock",
                    product.Id,
                    oldValues,
                    new { product.CurrentStock },
                    $"Adjusted stock: {model.MovementType}. {action} {model.Quantity} x {product.ItemName}. Stock: {oldStock} → {product.CurrentStock}",
                    product.ItemCode);

                TempData["SuccessMessage"] = $"Stock {action} {product.ItemName} successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
        }

        await PopulateProductDropdown();
        return View(model);
    }

    // GET: Stock/Transfer
    public async Task<IActionResult> Transfer()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get products for dropdown
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);

        // Get stores for dropdown (excluding current store)
        var stores = await _unitOfWork.Stores
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        var currentStoreId = HttpContext.Session.GetInt32("StoreId");

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();
        ViewBag.Stores = stores.Where(s => s.Id != currentStoreId).OrderBy(s => s.Name).ToList();

        return View(new StockTransferViewModel());
    }

    // POST: Stock/Transfer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(StockTransferViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var fromStoreId = HttpContext.Session.GetInt32("StoreId") ?? 0;

                // Get the product
                var product = await _unitOfWork.Products.GetByIdAsync(model.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("ProductId", "Product not found.");
                    await PopulateTransferDropdowns();
                    return View(model);
                }

                // Check if sufficient stock
                if (product.CurrentStock < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Current stock: {product.CurrentStock}");
                    await PopulateTransferDropdowns();
                    return View(model);
                }

                // Save old values for audit
                var oldStock = product.CurrentStock;
                var oldValues = new { product.CurrentStock, product.StoreId };

                // Update product stock (remove from current store)
                product.CurrentStock -= model.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);

                // Check if product exists in target store
                var targetProduct = await _unitOfWork.Products
                    .FirstOrDefaultAsync(p => p.OrganizationId == organizationId &&
                                             p.ItemCode == product.ItemCode &&
                                             p.StoreId == model.ToStoreId);

                int targetProductId;
                if (targetProduct != null)
                {
                    // Add to existing product in target store
                    targetProduct.CurrentStock += model.Quantity;
                    targetProduct.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Products.Update(targetProduct);
                    targetProductId = targetProduct.Id;

                    // Create stock movement for target store
                    var targetMovement = new StockMovement
                    {
                        ProductId = targetProduct.Id,
                        MovementDate = DateTime.UtcNow,
                        MovementType = MovementType.Transfer,
                        Quantity = model.Quantity,
                        UnitCost = product.CostPrice,
                        UnitPrice = product.SellingPrice,
                        Reference = $"TRANSFER-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                        Notes = $"Transfer from Store {fromStoreId} to Store {model.ToStoreId}. {model.Notes}",
                        StoreId = model.ToStoreId,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.StockMovements.AddAsync(targetMovement);
                }
                else
                {
                    // Create new product in target store
                    var newProduct = new Product
                    {
                        OrganizationId = organizationId,
                        ItemCode = product.ItemCode,
                        ItemName = product.ItemName,
                        Size = product.Size,
                        Category = product.Category,
                        CostPrice = product.CostPrice,
                        SellingPrice = product.SellingPrice,
                        PricingMode = product.PricingMode,
                        MarkupPercentage = product.MarkupPercentage,
                        CurrentStock = model.Quantity,
                        MinStockAlert = product.MinStockAlert,
                        ReorderPoint = product.ReorderPoint,
                        SupplierId = product.SupplierId,
                        Barcode = product.Barcode,
                        Weight = product.Weight,
                        Unit = product.Unit,
                        TaxRate = product.TaxRate,
                        IsActive = true,
                        StoreId = model.ToStoreId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };

                    await _unitOfWork.Products.AddAsync(newProduct);
                    await _unitOfWork.SaveChangesAsync();
                    targetProductId = newProduct.Id;

                    // Create stock movement for target store
                    var targetMovement = new StockMovement
                    {
                        ProductId = newProduct.Id,
                        MovementDate = DateTime.UtcNow,
                        MovementType = MovementType.Transfer,
                        Quantity = model.Quantity,
                        UnitCost = product.CostPrice,
                        UnitPrice = product.SellingPrice,
                        Reference = $"TRANSFER-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                        Notes = $"Transfer from Store {fromStoreId} to Store {model.ToStoreId}. {model.Notes}",
                        StoreId = model.ToStoreId,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.StockMovements.AddAsync(targetMovement);
                }

                // Create stock movement for source store
                var fromMovement = new StockMovement
                {
                    ProductId = product.Id,
                    MovementDate = DateTime.UtcNow,
                    MovementType = MovementType.Transfer,
                    Quantity = -model.Quantity,
                    UnitCost = product.CostPrice,
                    UnitPrice = product.SellingPrice,
                    Reference = $"TRANSFER-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                    Notes = $"Transfer from Store {fromStoreId} to Store {model.ToStoreId}. {model.Notes}",
                    StoreId = fromStoreId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.StockMovements.AddAsync(fromMovement);
                await _unitOfWork.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(
                    "Transfer",
                    "Stock",
                    product.Id,
                    oldValues,
                    new { product.CurrentStock, TargetStoreId = model.ToStoreId },
                    $"Transferred stock: {model.Quantity} x {product.ItemName}. Stock: {oldStock} → {product.CurrentStock}. To Store: {model.ToStoreId}",
                    product.ItemCode);

                TempData["SuccessMessage"] = $"Stock transferred successfully! {model.Quantity} x {product.ItemName} moved to target store.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
        }

        await PopulateTransferDropdowns();
        return View(model);
    }

    // GET: Stock/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var movement = await _unitOfWork.StockMovements.GetByIdAsync(id);
        if (movement == null)
        {
            return NotFound();
        }

        var model = new StockMovementViewModel
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            ProductName = movement.Product?.ItemName,
            ProductCode = movement.Product?.ItemCode,
            MovementType = movement.MovementType,
            Quantity = movement.Quantity,
            UnitCost = movement.UnitCost,
            UnitPrice = movement.UnitPrice,
            ExpiryDate = movement.ExpiryDate,
            BatchNumber = movement.BatchNumber,
            Reference = movement.Reference,
            Notes = movement.Notes,
            StoreId = movement.StoreId,
            CreatedBy = movement.CreatedBy.HasValue ? "User" : "System",
            CreatedAt = movement.CreatedAt
        };

        // Log view stock movement details
        await LogAuditAsync(
            "View",
            "StockMovement",
            movement.Id,
            null,
            null,
            $"Viewed stock movement details: {movement.MovementType} - {movement.Quantity} x {movement.Product?.ItemName}",
            movement.Reference);

        return View(model);
    }

    // Helper methods
    private async Task PopulateDropdowns()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);
        var suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);
        var cashAccounts = await _unitOfWork.CashAccounts
            .FindAsync(ca => ca.OrganizationId == organizationId && (ca.StoreId == storeId || ca.StoreId == null) && ca.IsActive);

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();
        ViewBag.Suppliers = suppliers.OrderBy(s => s.Name).ToList();
        ViewBag.CashAccounts = cashAccounts.OrderBy(ca => ca.AccountName).ToList();
        ViewBag.SourceTypes = new List<string> { "Purchase", "Transfer", "Return", "Adjustment", "Donation", "Consignment" };
    }

    private async Task PopulateProductDropdown()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();
    }

    private async Task PopulateTransferDropdowns()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId && p.IsActive);

        var stores = await _unitOfWork.Stores
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        var currentStoreId = HttpContext.Session.GetInt32("StoreId");

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();
        ViewBag.Stores = stores.Where(s => s.Id != currentStoreId).OrderBy(s => s.Name).ToList();
    }
}

public class StockTransferViewModel
{
    [Required(ErrorMessage = "Product is required")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Display(Name = "Product Name")]
    public string? ProductName { get; set; }

    [Display(Name = "Current Stock")]
    public int CurrentStock { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 999999, ErrorMessage = "Quantity must be greater than 0")]
    [Display(Name = "Quantity to Transfer")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Target store is required")]
    [Display(Name = "To Store")]
    public int ToStoreId { get; set; }

    [Display(Name = "Store Name")]
    public string? StoreName { get; set; }

    [Display(Name = "Notes")]
    [StringLength(500)]
    public string? Notes { get; set; }
}