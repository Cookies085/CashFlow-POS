using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
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

    // GET: Store/Details
    public async Task<IActionResult> Details(int id)
    {
        var store = await _unitOfWork.Stores.GetByIdAsync(id);
        if (store == null)
        {
            return NotFound();
        }

        // Log store details view
        await LogAuditAsync(
            "View",
            "StoreDetails",
            store.Id,
            null,
            store,
            $"Store details viewed: {store.Name} (Code: {store.StoreCode})",
            store.StoreCode);

        return Json(new
        {
            id = store.Id,
            name = store.Name,
            address = store.Address,
            phone = store.Phone,
            email = store.Email,
            storeCode = store.StoreCode,
            logoUrl = store.LogoUrl,
            themeColor = store.ThemeColor,
            themeMode = store.ThemeMode
        });
    }
}