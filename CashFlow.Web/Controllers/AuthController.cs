using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthController(
        IAuthService authService,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        IAuditService auditService) : base(auditService)
    {
        _authService = authService;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    // GET: Auth/Login
    [HttpGet]
    public async Task<IActionResult> Login(int? storeId, string? returnUrl = null)
    {
        // If user is already logged in, redirect to dashboard
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        // If no store selected, redirect to store selection
        if (!storeId.HasValue)
        {
            // Check if there's a remembered store
            var rememberedStoreId = Request.Cookies["RememberedStore"];
            if (!string.IsNullOrEmpty(rememberedStoreId) && int.TryParse(rememberedStoreId, out var rememberedId))
            {
                var store = await _unitOfWork.Stores.GetByIdAsync(rememberedId);
                if (store != null && store.IsActive)
                {
                    storeId = rememberedId;
                }
            }
        }

        if (!storeId.HasValue)
        {
            return RedirectToAction("Select", "Store");
        }

        // Get store details
        var selectedStore = await _unitOfWork.Stores.GetByIdAsync(storeId.Value);
        if (selectedStore == null || !selectedStore.IsActive)
        {
            TempData["ErrorMessage"] = "Store not found or inactive.";
            return RedirectToAction("Select", "Store");
        }

        // Store store context in session for the login page
        HttpContext.Session.SetInt32("SelectedStoreId", selectedStore.Id);
        HttpContext.Session.SetString("SelectedStoreName", selectedStore.Name);

        var model = new LoginViewModel
        {
            StoreId = selectedStore.Id,
            StoreName = selectedStore.Name,
            StoreLogo = selectedStore.LogoUrl,
            StoreThemeColor = selectedStore.ThemeColor ?? "#0d5c1f",
            RememberMe = false
        };

        ViewData["ReturnUrl"] = returnUrl;

        // Log login page view
        await LogAuditAsync(
            "View",
            "Login",
            null,
            null,
            new { StoreId = storeId, StoreName = selectedStore.Name },
            $"Login page viewed for store: {selectedStore.Name}",
            null);

        return View(model);
    }

    // POST: Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Get the selected store
        var store = await _unitOfWork.Stores.GetByIdAsync(model.StoreId);
        if (store == null || !store.IsActive)
        {
            ModelState.AddModelError("", "Store not found or inactive.");
            return View(model);
        }

        // Validate user credentials
        var result = await _authService.LoginAsync(model.Username, model.Password);

        if (!result.Success || result.User == null)
        {
            ModelState.AddModelError("", result.Message);
            // Log failed login attempt
            await LogAuditAsync(
                "LoginFailed",
                "User",
                null,
                null,
                new { Username = model.Username, StoreId = model.StoreId },
                $"Failed login attempt for username: {model.Username} at store: {store.Name}",
                null);
            return View(model);
        }

        // Validate user has access to this store
        var hasAccess = await ValidateUserStoreAccess(result.User.Id, model.StoreId);
        if (!hasAccess)
        {
            ModelState.AddModelError("", "You don't have access to this store.");

            // Log unauthorized access attempt
            await LogAuditAsync(
                "Unauthorized",
                "User",
                result.User.Id,
                null,
                new { StoreId = model.StoreId, StoreName = store.Name },
                $"Unauthorized access attempt: {result.User.Username} tried to access store: {store.Name}",
                null);

            return View(model);
        }

        // Create session
        HttpContext.Session.SetInt32("UserId", result.User.Id);
        HttpContext.Session.SetString("UserFullName", result.User.FullName);
        HttpContext.Session.SetString("UserRole", result.User.Role.ToString());
        HttpContext.Session.SetInt32("OrganizationId", result.User.OrganizationId);

        // Set the selected store
        HttpContext.Session.SetInt32("StoreId", model.StoreId);
        HttpContext.Session.SetString("StoreName", store.Name);
        HttpContext.Session.SetString("StoreCode", store.StoreCode ?? "");
        HttpContext.Session.SetString("StoreTheme", store.ThemeMode ?? "light");
        HttpContext.Session.SetString("StoreColor", store.ThemeColor ?? "#0d5c1f");

        // Update user's last store and login time
        result.User.LastStoreId = model.StoreId;
        result.User.LastLoginAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(result.User);
        await _unitOfWork.SaveChangesAsync();

        // Set session timeout
        HttpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));

        // Log successful login
        await LogAuditAsync(
            "Login",
            "User",
            result.User.Id,
            null,
            new { Username = result.User.Username, StoreId = model.StoreId, StoreName = store.Name, Role = result.User.Role },
            $"User {result.User.Username} logged in successfully to store: {store.Name}",
            null);

        // Redirect to return URL or home
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // GET: Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        // If user is already logged in, redirect to dashboard
        if (HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // POST: Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(
            model.Username,
            model.FullName,
            model.Email,
            model.Password,
            model.OrganizationName,
            model.Phone,
            model.Address);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            return View(model);
        }

        // Log successful registration
        await LogAuditAsync(
            "Register",
            "User",
            result.User?.Id,
            null,
            new { Username = model.Username, Organization = model.OrganizationName, Email = model.Email },
            $"New user registered: {model.Username} for organization: {model.OrganizationName}",
            null);

        // AuthService.RegisterAsync now returns the store code in the message
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    // GET: Auth/Logout
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var username = HttpContext.Session.GetString("UserFullName") ?? "Unknown";

        if (userId.HasValue)
        {
            // Log logout
            await LogAuditAsync(
                "Logout",
                "User",
                userId.Value,
                null,
                null,
                $"User {username} logged out",
                null);

            await _authService.LogoutAsync(userId.Value);
        }

        // Clear session
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }

    // GET: Auth/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        // Log access denied
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            var username = HttpContext.Session.GetString("UserFullName") ?? "Unknown";
            var requestedUrl = Request.Headers["Referer"].ToString();

            LogAuditAsync(
                "AccessDenied",
                "User",
                userId.Value,
                null,
                new { RequestedUrl = requestedUrl },
                $"Access denied for user: {username}. Attempted to access: {requestedUrl}",
                null).Wait();
        }

        return View();
    }

    // Helper method to validate user store access
    private async Task<bool> ValidateUserStoreAccess(int userId, int storeId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null) return false;

        // SuperAdmin and Admin have access to all stores
        if (user.Role == UserRole.SuperAdmin || user.Role == UserRole.Admin)
        {
            return true;
        }

        // Check if user is assigned to the store
        if (user.StoreId.HasValue && user.StoreId.Value == storeId)
        {
            return true;
        }

        // Check if user has StoreId null (all stores access)
        if (!user.StoreId.HasValue && user.Role == UserRole.Manager)
        {
            return true;
        }

        return false;
    }

    // Helper method to generate store code
    private string GenerateStoreCode(string organizationName)
    {
        // Take first 3 letters of organization name + random 3 digits
        var prefix = organizationName.Length >= 3 ?
            organizationName.Substring(0, 3).ToUpper() :
            organizationName.ToUpper().PadRight(3, 'X');
        var random = new Random().Next(100, 999);
        return $"{prefix}{random}";
    }
}