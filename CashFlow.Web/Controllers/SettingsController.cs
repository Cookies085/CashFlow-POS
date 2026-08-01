using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Web.Controllers;

public class SettingsController : BaseController
{
    private readonly ISettingsService _settingsService;
    private readonly IUnitOfWork _unitOfWork;

    public SettingsController(ISettingsService settingsService, IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _settingsService = settingsService;
        _unitOfWork = unitOfWork;
    }

    // GET: Settings
    public async Task<IActionResult> Index()
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var settings = await _settingsService.GetSettingsAsync(organizationId, storeId);

        // Map DTO to ViewModel
        var model = new SettingsViewModel
        {
            StoreName = settings.StoreName,
            StoreCode = settings.StoreCode,
            StoreAddress = settings.StoreAddress,
            StorePhone = settings.StorePhone,
            StoreEmail = settings.StoreEmail,
            StoreLogo = settings.StoreLogo,
            CompanyName = settings.CompanyName,
            RegistrationNumber = settings.RegistrationNumber,
            VatNumber = settings.VatNumber,
            CompanyAddress = settings.CompanyAddress,
            VatRate = settings.VatRate,
            TaxMethod = settings.TaxMethod,
            TaxNumberLabel = settings.TaxNumberLabel,
            ReceiptHeader = settings.ReceiptHeader,
            ReceiptFooter = settings.ReceiptFooter,
            ShowLogoOnReceipt = settings.ShowLogoOnReceipt,
            ReceiptPaperSize = settings.ReceiptPaperSize,
            ShowBarcodeOnReceipt = settings.ShowBarcodeOnReceipt,
            CurrencySymbol = settings.CurrencySymbol,
            CurrencyCode = settings.CurrencyCode,
            DecimalPlaces = settings.DecimalPlaces,
            ThousandsSeparator = settings.ThousandsSeparator,
            DecimalSeparator = settings.DecimalSeparator,
            CurrencyFormat = settings.CurrencyFormat,
            ThemeMode = settings.ThemeMode,
            PrimaryColor = settings.PrimaryColor,
            AccentColor = settings.AccentColor,
            SidebarColor = settings.SidebarColor
        };

        // Log view settings
        await LogAuditAsync(
            "View",
            "Settings",
            null,
            null,
            null,
            "Viewed system settings",
            null);

        return View(model);
    }

    // POST: Settings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

        // Save old settings for audit
        var oldSettings = await _settingsService.GetSettingsAsync(organizationId, storeId);

        // Map ViewModel to DTO
        var settingsDto = new SettingsDto
        {
            StoreName = model.StoreName,
            StoreCode = model.StoreCode,
            StoreAddress = model.StoreAddress,
            StorePhone = model.StorePhone,
            StoreEmail = model.StoreEmail,
            StoreLogo = model.StoreLogo,
            CompanyName = model.CompanyName,
            RegistrationNumber = model.RegistrationNumber,
            VatNumber = model.VatNumber,
            CompanyAddress = model.CompanyAddress,
            VatRate = model.VatRate,
            TaxMethod = model.TaxMethod,
            TaxNumberLabel = model.TaxNumberLabel,
            ReceiptHeader = model.ReceiptHeader,
            ReceiptFooter = model.ReceiptFooter,
            ShowLogoOnReceipt = model.ShowLogoOnReceipt,
            ReceiptPaperSize = model.ReceiptPaperSize,
            ShowBarcodeOnReceipt = model.ShowBarcodeOnReceipt,
            CurrencySymbol = model.CurrencySymbol,
            CurrencyCode = model.CurrencyCode,
            DecimalPlaces = model.DecimalPlaces,
            ThousandsSeparator = model.ThousandsSeparator,
            DecimalSeparator = model.DecimalSeparator,
            CurrencyFormat = model.CurrencyFormat,
            ThemeMode = model.ThemeMode,
            PrimaryColor = model.PrimaryColor,
            AccentColor = model.AccentColor,
            SidebarColor = model.SidebarColor
        };

        var success = await _settingsService.SaveSettingsAsync(organizationId, storeId, settingsDto, userId);

        if (success)
        {
            // Update session with store name if changed
            if (!string.IsNullOrEmpty(model.StoreName))
            {
                HttpContext.Session.SetString("StoreName", model.StoreName);
            }

            // Update session with theme if changed
            if (!string.IsNullOrEmpty(model.ThemeMode))
            {
                HttpContext.Session.SetString("StoreTheme", model.ThemeMode);
            }

            // Update session with colors if changed
            if (!string.IsNullOrEmpty(model.PrimaryColor))
            {
                HttpContext.Session.SetString("StoreColor", model.PrimaryColor);
            }

            // Update store entity with code if set
            if (!string.IsNullOrEmpty(model.StoreCode) && storeId.HasValue)
            {
                var store = await _unitOfWork.Stores.GetByIdAsync(storeId.Value);
                if (store != null)
                {
                    store.StoreCode = model.StoreCode;
                    store.ThemeColor = model.PrimaryColor;
                    store.ThemeMode = model.ThemeMode;
                    store.LogoUrl = model.StoreLogo;
                    store.Name = model.StoreName;
                    store.Address = model.StoreAddress;
                    store.Phone = model.StorePhone;
                    store.Email = model.StoreEmail;
                    _unitOfWork.Stores.Update(store);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // Build audit description of what changed
            var changes = new List<string>();
            if (oldSettings.StoreName != model.StoreName) changes.Add($"Store Name: {oldSettings.StoreName} → {model.StoreName}");
            if (oldSettings.StoreCode != model.StoreCode) changes.Add($"Store Code: {oldSettings.StoreCode} → {model.StoreCode}");
            if (oldSettings.VatRate != model.VatRate) changes.Add($"VAT Rate: {oldSettings.VatRate}% → {model.VatRate}%");
            if (oldSettings.ThemeMode != model.ThemeMode) changes.Add($"Theme: {oldSettings.ThemeMode} → {model.ThemeMode}");
            if (oldSettings.PrimaryColor != model.PrimaryColor) changes.Add($"Primary Color: {oldSettings.PrimaryColor} → {model.PrimaryColor}");
            if (oldSettings.CurrencySymbol != model.CurrencySymbol) changes.Add($"Currency: {oldSettings.CurrencySymbol} → {model.CurrencySymbol}");

            var changeDescription = changes.Count > 0
                ? $"Updated settings: {string.Join(", ", changes)}"
                : "Updated settings (no visible changes)";

            // Log audit
            await LogAuditAsync(
                "Update",
                "Settings",
                storeId,
                oldSettings,
                settingsDto,
                changeDescription,
                null);

            TempData["SuccessMessage"] = "Settings saved successfully!";
        }
        else
        {
            ModelState.AddModelError("", "Failed to save settings. Please try again.");
        }

        return View(model);
    }

    // GET: Settings/Theme
    public async Task<IActionResult> Theme()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        var settings = await _settingsService.GetSettingsAsync(organizationId, storeId);

        // Log theme view
        await LogAuditAsync(
            "View",
            "Theme",
            null,
            null,
            null,
            "Viewed theme settings",
            null);

        return Json(new
        {
            themeMode = settings.ThemeMode,
            primaryColor = settings.PrimaryColor,
            accentColor = settings.AccentColor,
            sidebarColor = settings.SidebarColor
        });
    }

    // POST: Settings/Theme
    [HttpPost]
    public async Task<IActionResult> Theme(string themeMode, string primaryColor, string accentColor, string sidebarColor)
    {
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get old theme settings for audit
        var oldSettings = await _settingsService.GetSettingsAsync(organizationId, storeId);
        var oldTheme = new { oldSettings.ThemeMode, oldSettings.PrimaryColor, oldSettings.AccentColor, oldSettings.SidebarColor };

        await _settingsService.SetSettingValueAsync(organizationId, "ThemeMode", themeMode, userId, storeId, "Theme");
        await _settingsService.SetSettingValueAsync(organizationId, "PrimaryColor", primaryColor, userId, storeId, "Theme");
        await _settingsService.SetSettingValueAsync(organizationId, "AccentColor", accentColor, userId, storeId, "Theme");
        await _settingsService.SetSettingValueAsync(organizationId, "SidebarColor", sidebarColor, userId, storeId, "Theme");

        // Update session
        HttpContext.Session.SetString("StoreTheme", themeMode);
        HttpContext.Session.SetString("StoreColor", primaryColor);

        // Log audit
        var newTheme = new { ThemeMode = themeMode, PrimaryColor = primaryColor, AccentColor = accentColor, SidebarColor = sidebarColor };
        await LogAuditAsync(
            "Update",
            "Theme",
            storeId,
            oldTheme,
            newTheme,
            $"Updated theme: Mode={themeMode}, Colors updated",
            null);

        return Json(new { success = true, message = "Theme updated successfully!" });
    }

    // POST: Settings/UploadLogo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadLogo(IFormFile logoFile)
    {
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        if (logoFile == null || logoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a logo file.";
            return RedirectToAction("Index");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/svg+xml", "image/webp" };
        if (!allowedTypes.Contains(logoFile.ContentType))
        {
            TempData["ErrorMessage"] = "Please upload a valid image file (JPEG, PNG, GIF, SVG, or WebP).";
            return RedirectToAction("Index");
        }

        // Validate file size (max 2MB)
        if (logoFile.Length > 2 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "Logo file size must be less than 2MB.";
            return RedirectToAction("Index");
        }

        try
        {
            // Create unique filename
            var fileName = $"logo_{storeId ?? organizationId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(logoFile.FileName)}";
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logos");

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var filePath = Path.Combine(uploadPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            // Update settings with logo URL
            var logoUrl = $"/images/logos/{fileName}";
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            await _settingsService.SetSettingValueAsync(organizationId, "StoreLogo", logoUrl, userId, storeId, "Store");

            // Update store entity
            if (storeId.HasValue)
            {
                var store = await _unitOfWork.Stores.GetByIdAsync(storeId.Value);
                if (store != null)
                {
                    store.LogoUrl = logoUrl;
                    _unitOfWork.Stores.Update(store);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Logo uploaded successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error uploading logo: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    // POST: Settings/RemoveLogo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLogo()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

        // Get current logo URL
        var logoUrl = await _settingsService.GetSettingValueAsync(organizationId, "StoreLogo", storeId);

        if (!string.IsNullOrEmpty(logoUrl))
        {
            // Delete file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", logoUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Remove from settings
            await _settingsService.SetSettingValueAsync(organizationId, "StoreLogo", "", userId, storeId, "Store");

            // Remove from store entity
            if (storeId.HasValue)
            {
                var store = await _unitOfWork.Stores.GetByIdAsync(storeId.Value);
                if (store != null)
                {
                    store.LogoUrl = null;
                    _unitOfWork.Stores.Update(store);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = "Logo removed successfully!";
        }

        return RedirectToAction("Index");
    }
}