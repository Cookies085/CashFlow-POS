using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class SuppliersController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public SuppliersController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Suppliers
    public async Task<IActionResult> Index(string? searchTerm, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get all suppliers for the organization
        var suppliers = await _unitOfWork.Suppliers
            .FindAsync(s => s.OrganizationId == organizationId);

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            suppliers = suppliers.Where(s =>
                s.Name.ToLower().Contains(searchTerm) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(searchTerm)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchTerm)) ||
                (s.Phone != null && s.Phone.Contains(searchTerm)));
        }

        // Order by name
        suppliers = suppliers.OrderBy(s => s.Name);

        // Get product counts for each supplier
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId);

        // Convert to ViewModel
        var supplierViewModels = suppliers.Select(s => new SupplierViewModel
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            TaxNumber = s.TaxNumber,
            IsActive = s.IsActive,
            ProductCount = products.Count(p => p.SupplierId == s.Id),
            ProductNames = string.Join(", ", products
                .Where(p => p.SupplierId == s.Id)
                .Take(3)
                .Select(p => p.ItemName))
                + (products.Count(p => p.SupplierId == s.Id) > 3 ? "..." : "")
        }).ToList();

        // Pagination
        int pageSize = 10;
        int totalCount = supplierViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedSuppliers = supplierViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new SupplierListViewModel
        {
            Suppliers = pagedSuppliers,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm
        };

        ViewBag.SearchTerm = searchTerm;

        // Log view suppliers action
        await LogAuditAsync(
            "View",
            "Suppliers",
            null,
            null,
            null,
            $"Viewed suppliers list. Page: {page}, Total suppliers: {totalCount}",
            null);

        return View(model);
    }

    // GET: Suppliers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }

        // Get products for this supplier
        var products = await _unitOfWork.Products
            .FindAsync(p => p.SupplierId == id);

        var model = new SupplierViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxNumber = supplier.TaxNumber,
            IsActive = supplier.IsActive,
            ProductCount = products.Count(),
            ProductNames = string.Join(", ", products.Select(p => p.ItemName))
        };

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();

        // Log view supplier details
        await LogAuditAsync(
            "View",
            "Supplier",
            supplier.Id,
            null,
            null,
            $"Viewed supplier details: {supplier.Name}",
            supplier.Name);

        return View(model);
    }

    // GET: Suppliers/Create
    public IActionResult Create()
    {
        return View(new SupplierViewModel());
    }

    // POST: Suppliers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierViewModel model)
    {
        if (ModelState.IsValid)
        {
            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

            // Check if supplier name already exists
            var existingSupplier = await _unitOfWork.Suppliers
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Name == model.Name);

            if (existingSupplier != null)
            {
                ModelState.AddModelError("Name", "A supplier with this name already exists.");
                return View(model);
            }

            var supplier = new Supplier
            {
                OrganizationId = organizationId,
                Name = model.Name,
                ContactPerson = model.ContactPerson,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                TaxNumber = model.TaxNumber,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(
                "Create",
                "Supplier",
                supplier.Id,
                null,
                supplier,
                $"Created supplier: {supplier.Name}",
                supplier.Name);

            TempData["SuccessMessage"] = "Supplier created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Suppliers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }

        var model = new SupplierViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            TaxNumber = supplier.TaxNumber,
            IsActive = supplier.IsActive
        };

        return View(model);
    }

    // POST: Suppliers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
                if (supplier == null)
                {
                    return NotFound();
                }

                // Check if supplier name already exists (excluding current supplier)
                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var existingSupplier = await _unitOfWork.Suppliers
                    .FirstOrDefaultAsync(s => s.OrganizationId == organizationId &&
                                             s.Name == model.Name &&
                                             s.Id != id);

                if (existingSupplier != null)
                {
                    ModelState.AddModelError("Name", "A supplier with this name already exists.");
                    return View(model);
                }

                // Save old values for audit
                var oldSupplier = new
                {
                    supplier.Name,
                    supplier.ContactPerson,
                    supplier.Phone,
                    supplier.Email,
                    supplier.Address,
                    supplier.TaxNumber,
                    supplier.IsActive
                };

                supplier.Name = model.Name;
                supplier.ContactPerson = model.ContactPerson;
                supplier.Phone = model.Phone;
                supplier.Email = model.Email;
                supplier.Address = model.Address;
                supplier.TaxNumber = model.TaxNumber;
                supplier.IsActive = model.IsActive;

                _unitOfWork.Suppliers.Update(supplier);
                await _unitOfWork.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(
                    "Edit",
                    "Supplier",
                    supplier.Id,
                    oldSupplier,
                    supplier,
                    $"Updated supplier: {supplier.Name}",
                    supplier.Name);

                TempData["SuccessMessage"] = "Supplier updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while updating the supplier.");
            }
        }

        return View(model);
    }

    // GET: Suppliers/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }

        // Check if supplier has products
        var productCount = (await _unitOfWork.Products
            .FindAsync(p => p.SupplierId == id))
            .Count();

        var model = new SupplierViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            IsActive = supplier.IsActive,
            ProductCount = productCount
        };

        if (productCount > 0)
        {
            ViewBag.Warning = $"This supplier has {productCount} products linked to it. Deleting will unlink these products.";
        }

        return View(model);
    }

    // POST: Suppliers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
        if (supplier != null)
        {
            // Save old values for audit
            var oldSupplier = new
            {
                supplier.Name,
                supplier.ContactPerson,
                supplier.Phone,
                supplier.Email,
                supplier.IsActive
            };

            // Check if supplier has products
            var products = await _unitOfWork.Products
                .FindAsync(p => p.SupplierId == id);

            // Unlink products
            foreach (var product in products)
            {
                product.SupplierId = null;
                _unitOfWork.Products.Update(product);
            }

            // Soft delete supplier
            supplier.IsActive = false;
            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(
                "Delete",
                "Supplier",
                supplier.Id,
                oldSupplier,
                null,
                $"Deleted supplier: {supplier.Name}. Unlinked {products.Count()} products.",
                supplier.Name);

            TempData["SuccessMessage"] = "Supplier deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Suppliers/GetProducts/5
    public async Task<IActionResult> GetProducts(int id)
    {
        var products = await _unitOfWork.Products
            .FindAsync(p => p.SupplierId == id);

        // Log API call
        await LogAuditAsync(
            "API",
            "SupplierProducts",
            id,
            null,
            null,
            $"Retrieved products for supplier ID: {id}, Count: {products.Count()}",
            null);

        return Json(products.Select(p => new
        {
            p.Id,
            p.ItemCode,
            p.ItemName,
            p.SellingPrice,
            p.CurrentStock,
            p.IsActive
        }));
    }
}