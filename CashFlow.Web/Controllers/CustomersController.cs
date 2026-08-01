using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class CustomersController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomersController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Customers
    public async Task<IActionResult> Index(string? searchTerm, string? tierFilter, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get all customers for the organization
        var customers = await _unitOfWork.Customers
            .FindAsync(c => c.OrganizationId == organizationId);

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            customers = customers.Where(c =>
                c.FirstName.ToLower().Contains(searchTerm) ||
                c.LastName.ToLower().Contains(searchTerm) ||
                c.Phone.Contains(searchTerm) ||
                (c.Email != null && c.Email.ToLower().Contains(searchTerm)));
        }

        // Apply tier filter
        if (!string.IsNullOrEmpty(tierFilter))
        {
            customers = customers.Where(c => c.PointsTier == tierFilter);
        }

        // Get unique tiers for filter dropdown
        var tiers = customers.Select(c => c.PointsTier).Distinct().OrderBy(t => t).ToList();

        // Convert to ViewModel
        var customerViewModels = customers.Select(c => new CustomerViewModel
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            Phone = c.Phone,
            Address = c.Address,
            LoyaltyPoints = c.LoyaltyPoints,
            PointsTier = c.PointsTier,
            TotalSpent = c.TotalSpent,
            LastPurchaseDate = c.LastPurchaseDate,
            DateOfBirth = c.DateOfBirth,
            Notes = c.Notes,
            IsActive = c.IsActive,
            StoreId = c.StoreId
        }).OrderByDescending(c => c.TotalSpent).ToList();

        // Pagination
        int pageSize = 10;
        int totalCount = customerViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedCustomers = customerViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new CustomerListViewModel
        {
            Customers = pagedCustomers,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            TierFilter = tierFilter
        };

        ViewBag.Tiers = tiers;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentTier = tierFilter;

        // Log view customers
        await LogAuditAsync(
            "View",
            "Customers",
            null,
            null,
            null,
            $"Viewed customers list. Page: {page}, Total customers: {totalCount}",
            null);

        return View(model);
    }

    // GET: Customers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        // Get customer's purchase history
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.CustomerId == id);

        var model = new CustomerViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            LoyaltyPoints = customer.LoyaltyPoints,
            PointsTier = customer.PointsTier,
            TotalSpent = customer.TotalSpent,
            LastPurchaseDate = customer.LastPurchaseDate,
            DateOfBirth = customer.DateOfBirth,
            Notes = customer.Notes,
            IsActive = customer.IsActive
        };

        ViewBag.Sales = sales.OrderByDescending(s => s.SaleDate).Take(10).ToList();
        ViewBag.TotalSales = sales.Count();

        // Log view customer details
        await LogAuditAsync(
            "View",
            "Customer",
            customer.Id,
            null,
            customer,
            $"Viewed customer details: {customer.FirstName} {customer.LastName} (Phone: {customer.Phone})",
            customer.Phone);

        return View(model);
    }

    // GET: Customers/Create
    public IActionResult Create()
    {
        return View(new CustomerViewModel());
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerViewModel model)
    {
        if (ModelState.IsValid)
        {
            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
            var storeId = HttpContext.Session.GetInt32("StoreId");

            // Check if phone already exists
            var existingCustomer = await _unitOfWork.Customers
                .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Phone == model.Phone);

            if (existingCustomer != null)
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
                return View(model);
            }

            var customer = new Customer
            {
                OrganizationId = organizationId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                LoyaltyPoints = 0,
                PointsTier = "Bronze",
                TotalSpent = 0,
                DateOfBirth = model.DateOfBirth,
                Notes = model.Notes,
                IsActive = true,
                StoreId = storeId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(
                "Create",
                "Customer",
                customer.Id,
                null,
                customer,
                $"Created customer: {customer.FirstName} {customer.LastName} (Phone: {customer.Phone})",
                customer.Phone);

            TempData["SuccessMessage"] = "Customer created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Customers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Address = customer.Address,
            LoyaltyPoints = customer.LoyaltyPoints,
            PointsTier = customer.PointsTier,
            TotalSpent = customer.TotalSpent,
            LastPurchaseDate = customer.LastPurchaseDate,
            DateOfBirth = customer.DateOfBirth,
            Notes = customer.Notes,
            IsActive = customer.IsActive
        };

        return View(model);
    }

    // POST: Customers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(id);
                if (customer == null)
                {
                    return NotFound();
                }

                // Check if phone already exists (excluding current customer)
                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var existingCustomer = await _unitOfWork.Customers
                    .FirstOrDefaultAsync(c => c.OrganizationId == organizationId &&
                                             c.Phone == model.Phone &&
                                             c.Id != id);

                if (existingCustomer != null)
                {
                    ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
                    return View(model);
                }

                // Save old values for audit
                var oldCustomer = new
                {
                    customer.FirstName,
                    customer.LastName,
                    customer.Email,
                    customer.Phone,
                    customer.Address,
                    customer.IsActive
                };

                customer.FirstName = model.FirstName;
                customer.LastName = model.LastName;
                customer.Email = model.Email;
                customer.Phone = model.Phone;
                customer.Address = model.Address;
                customer.DateOfBirth = model.DateOfBirth;
                customer.Notes = model.Notes;
                customer.IsActive = model.IsActive;

                _unitOfWork.Customers.Update(customer);
                await _unitOfWork.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(
                    "Edit",
                    "Customer",
                    customer.Id,
                    oldCustomer,
                    customer,
                    $"Updated customer: {customer.FirstName} {customer.LastName} (Phone: {customer.Phone})",
                    customer.Phone);

                TempData["SuccessMessage"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while updating the customer.");
            }
        }

        return View(model);
    }

    // GET: Customers/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone,
            TotalSpent = customer.TotalSpent,
            LoyaltyPoints = customer.LoyaltyPoints,
            IsActive = customer.IsActive
        };

        return View(model);
    }

    // POST: Customers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer != null)
        {
            // Save old values for audit
            var oldCustomer = new
            {
                customer.FirstName,
                customer.LastName,
                customer.Phone,
                customer.IsActive
            };

            // Soft delete
            customer.IsActive = false;
            _unitOfWork.Customers.Update(customer);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(
                "Delete",
                "Customer",
                customer.Id,
                oldCustomer,
                null,
                $"Deleted customer: {customer.FirstName} {customer.LastName} (Phone: {customer.Phone})",
                customer.Phone);

            TempData["SuccessMessage"] = "Customer deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }
}