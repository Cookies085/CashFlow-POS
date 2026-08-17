using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.Filters;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CashFlow.Web.Controllers;

public class StoreController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StoreController(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GenerateStoreCode(string storeName)
    {
        var prefix = storeName.Length >= 3 ? storeName.Substring(0, 3).ToUpper() : storeName.ToUpper().PadRight(3, 'X');
        var random = new Random().Next(100, 999);
        return $"{prefix}{random}";
    }

    // GET: Store/Index - Manage stores (Admin/SuperAdmin only)
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public async Task<IActionResult> Index()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var stores = await _unitOfWork.Stores
            .FindAsync(s => s.OrganizationId == organizationId);

        var viewModel = stores.Select(s => new StoreViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Address = s.Address,
            Phone = s.Phone,
            Email = s.Email,
            StoreCode = s.StoreCode,
            LogoUrl = s.LogoUrl,
            ThemeColor = s.ThemeColor ?? "#0d5c1f",
            ThemeMode = s.ThemeMode ?? "light",
            IsActive = s.IsActive
        }).ToList();

        return View(viewModel);
    }

    // GET: Store/Create
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public IActionResult Create()
    {
        return View(new StoreViewModel());
    }

    // POST: Store/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public async Task<IActionResult> Create(StoreViewModel model)
    {
        if (ModelState.IsValid)
        {
            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            // Check if store code already exists
            var existingStore = await _unitOfWork.Stores
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.StoreCode == model.StoreCode);
            if (existingStore != null)
            {
                ModelState.AddModelError("StoreCode", "A store with this code already exists.");
                return View(model);
            }

            // Generate store code if not provided
            if (string.IsNullOrEmpty(model.StoreCode))
            {
                model.StoreCode = GenerateStoreCode(model.Name);
            }

            var store = new Store
            {
                OrganizationId = organizationId,
                Name = model.Name,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.Email,
                StoreCode = model.StoreCode,
                LogoUrl = model.LogoUrl,
                ThemeColor = model.ThemeColor ?? "#0d5c1f",
                ThemeMode = model.ThemeMode ?? "light",
                Timezone = "Africa/Johannesburg",
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            await _unitOfWork.Stores.AddAsync(store);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Store '{store.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Store/Edit/5
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var store = await _unitOfWork.Stores.GetByIdAsync(id);
        if (store == null)
        {
            return NotFound();
        }

        var model = new StoreViewModel
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            Phone = store.Phone,
            Email = store.Email,
            StoreCode = store.StoreCode,
            LogoUrl = store.LogoUrl,
            ThemeColor = store.ThemeColor ?? "#0d5c1f",
            ThemeMode = store.ThemeMode ?? "light",
            IsActive = store.IsActive
        };

        return View(model);
    }

    // POST: Store/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public async Task<IActionResult> Edit(int id, StoreViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var store = await _unitOfWork.Stores.GetByIdAsync(id);
            if (store == null)
            {
                return NotFound();
            }

            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

            // Check if store code already exists (excluding current store)
            var existingStore = await _unitOfWork.Stores
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.StoreCode == model.StoreCode && s.Id != id);
            if (existingStore != null)
            {
                ModelState.AddModelError("StoreCode", "A store with this code already exists.");
                return View(model);
            }

            // Update store
            store.Name = model.Name;
            store.Address = model.Address;
            store.Phone = model.Phone;
            store.Email = model.Email;
            store.StoreCode = model.StoreCode;
            store.LogoUrl = model.LogoUrl;
            store.ThemeColor = model.ThemeColor ?? "#0d5c1f";
            store.ThemeMode = model.ThemeMode ?? "light";
            store.IsActive = model.IsActive;

            _unitOfWork.Stores.Update(store);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Store '{store.Name}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: Store/Details/5
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public async Task<IActionResult> Details(int id)
    {
        var store = await _unitOfWork.Stores.GetByIdAsync(id);
        if (store == null)
        {
            return NotFound();
        }

        var model = new StoreViewModel
        {
            Id = store.Id,
            Name = store.Name,
            Address = store.Address,
            Phone = store.Phone,
            Email = store.Email,
            StoreCode = store.StoreCode,
            LogoUrl = store.LogoUrl,
            ThemeColor = store.ThemeColor ?? "#0d5c1f",
            ThemeMode = store.ThemeMode ?? "light",
            IsActive = store.IsActive
        };

        return View(model);
    }

    // POST: Store/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var store = await _unitOfWork.Stores.GetByIdAsync(id);
        if (store != null)
        {
            // Soft delete - just deactivate
            store.IsActive = false;
            _unitOfWork.Stores.Update(store);
            await _unitOfWork.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Store '{store.Name}' deactivated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Store/Select
    public async Task<IActionResult> Select()
    {
        // Check if user is already logged in
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // If no organization in session, user hasn't registered yet
        if (organizationId == 0)
        {
            // Get first organization (should only be one for new users)
            var organizations = await _unitOfWork.Organizations.GetAllAsync();
            var org = organizations.FirstOrDefault();
            if (org != null)
            {
                organizationId = org.Id;
                HttpContext.Session.SetInt32("OrganizationId", organizationId);
            }
            else
            {
                // No organization - redirect to register
                return RedirectToAction("Register", "Auth");
            }
        }

        // Get all active stores for the organization
        var stores = await _unitOfWork.Stores
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        var model = new StoreSelectionViewModel
        {
            Stores = stores.Select(s => new StoreViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Phone = s.Phone,
                Email = s.Email,
                StoreCode = s.StoreCode,
                LogoUrl = s.LogoUrl,
                ThemeColor = s.ThemeColor ?? "#0d5c1f",
                ThemeMode = s.ThemeMode ?? "light",
                IsActive = s.IsActive
            }).ToList()
        };

        // Log store selection page view
        await LogAuditAsync(
            "View",
            "StoreSelection",
            null,
            null,
            null,
            $"Store selection page viewed. {stores.Count()} stores available.",
            null);

        return View(model);
    }

    // POST: Store/Select
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(StoreSelectionViewModel model)
    {
        if (ModelState.IsValid)
        {
            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

            Store? store = null;

            // Try to find by store code first
            if (!string.IsNullOrEmpty(model.StoreCode))
            {
                store = await _unitOfWork.Stores
                    .FirstOrDefaultAsync(s => s.OrganizationId == organizationId &&
                                             s.StoreCode == model.StoreCode &&
                                             s.IsActive);
            }

            // If not found by code, try by ID from selection
            if (store == null && model.Stores.Any())
            {
                var selectedStore = model.Stores.FirstOrDefault(s => s.StoreCode == model.StoreCode);
                if (selectedStore != null)
                {
                    store = await _unitOfWork.Stores.GetByIdAsync(selectedStore.Id);
                }
            }

            if (store == null)
            {
                ModelState.AddModelError("StoreCode", "Store not found. Please check the store code.");
                return View(model);
            }

            // Store store info in session
            HttpContext.Session.SetInt32("SelectedStoreId", store.Id);
            HttpContext.Session.SetString("SelectedStoreName", store.Name);
            HttpContext.Session.SetString("SelectedStoreCode", store.StoreCode ?? "");
            HttpContext.Session.SetString("SelectedStoreTheme", store.ThemeMode ?? "light");
            HttpContext.Session.SetString("SelectedStoreColor", store.ThemeColor ?? "#0d5c1f");

            // If remember store, save to cookie
            if (model.RememberStore)
            {
                Response.Cookies.Append("RememberedStore", store.Id.ToString(), new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(30),
                    HttpOnly = true,
                    Secure = true
                });
            }

            // Log store selection
            await LogAuditAsync(
                "Select",
                "Store",
                store.Id,
                null,
                store,
                $"Store selected: {store.Name} (Code: {store.StoreCode})",
                store.StoreCode);

            // Redirect to login with store context
            return RedirectToAction("Login", "Auth", new { storeId = store.Id });
        }

        return View(model);
    }
}