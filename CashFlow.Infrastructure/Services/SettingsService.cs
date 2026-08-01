using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using System.Text.Json;

namespace CashFlow.Infrastructure.Services;

public interface ISettingsService
{
    Task<SettingsDto> GetSettingsAsync(int organizationId, int? storeId);
    Task<bool> SaveSettingsAsync(int organizationId, int? storeId, SettingsDto model, int userId);
    Task<T?> GetSettingValueAsync<T>(int organizationId, string key, int? storeId = null);
    Task<string?> GetSettingValueAsync(int organizationId, string key, int? storeId = null);
    Task<bool> SetSettingValueAsync(int organizationId, string key, string value, int userId, int? storeId = null, string? group = null);
}

public class SettingsDto
{
    public string StoreName { get; set; } = string.Empty;
    public string? StoreCode { get; set; }
    public string? StoreAddress { get; set; }
    public string? StorePhone { get; set; }
    public string? StoreEmail { get; set; }
    public string? StoreLogo { get; set; }
    public string? CompanyName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? CompanyAddress { get; set; }
    public decimal VatRate { get; set; } = 15;
    public string TaxMethod { get; set; } = "Inclusive";
    public string? TaxNumberLabel { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool ShowLogoOnReceipt { get; set; } = true;
    public string ReceiptPaperSize { get; set; } = "80mm";
    public bool ShowBarcodeOnReceipt { get; set; } = true;
    public string CurrencySymbol { get; set; } = "R";
    public string CurrencyCode { get; set; } = "ZAR";
    public int DecimalPlaces { get; set; } = 2;
    public string ThousandsSeparator { get; set; } = ",";
    public string DecimalSeparator { get; set; } = ".";
    public string CurrencyFormat { get; set; } = "SymbolFirst";
    public string ThemeMode { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#0d5c1f";
    public string AccentColor { get; set; } = "#22c55e";
    public string SidebarColor { get; set; } = "#0d5c1f";
}

public class SettingsService : ISettingsService
{
    private readonly IUnitOfWork _unitOfWork;

    public SettingsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SettingsDto> GetSettingsAsync(int organizationId, int? storeId)
    {
        var settings = await _unitOfWork.Settings
            .FindAsync(s => s.OrganizationId == organizationId && (s.StoreId == storeId || s.StoreId == null));

        var dict = settings.ToDictionary(s => s.Key, s => s.Value);

        return new SettingsDto
        {
            // Store Settings
            StoreName = GetValue(dict, "StoreName", "My Store"),
            StoreCode = GetValue(dict, "StoreCode", ""),
            StoreAddress = GetValue(dict, "StoreAddress", ""),
            StorePhone = GetValue(dict, "StorePhone", ""),
            StoreEmail = GetValue(dict, "StoreEmail", ""),
            StoreLogo = GetValue(dict, "StoreLogo", ""),

            // Company Settings
            CompanyName = GetValue(dict, "CompanyName", ""),
            RegistrationNumber = GetValue(dict, "RegistrationNumber", ""),
            VatNumber = GetValue(dict, "VatNumber", ""),
            CompanyAddress = GetValue(dict, "CompanyAddress", ""),

            // Tax Settings
            VatRate = GetValue<decimal>(dict, "VatRate", 15),
            TaxMethod = GetValue(dict, "TaxMethod", "Inclusive"),
            TaxNumberLabel = GetValue(dict, "TaxNumberLabel", "VAT Number"),

            // Receipt Settings
            ReceiptHeader = GetValue(dict, "ReceiptHeader", "Thank you for your business!"),
            ReceiptFooter = GetValue(dict, "ReceiptFooter", "Visit us again!"),
            ShowLogoOnReceipt = GetValue<bool>(dict, "ShowLogoOnReceipt", true),
            ReceiptPaperSize = GetValue(dict, "ReceiptPaperSize", "80mm"),
            ShowBarcodeOnReceipt = GetValue<bool>(dict, "ShowBarcodeOnReceipt", true),

            // Currency Settings
            CurrencySymbol = GetValue(dict, "CurrencySymbol", "R"),
            CurrencyCode = GetValue(dict, "CurrencyCode", "ZAR"),
            DecimalPlaces = GetValue<int>(dict, "DecimalPlaces", 2),
            ThousandsSeparator = GetValue(dict, "ThousandsSeparator", ","),
            DecimalSeparator = GetValue(dict, "DecimalSeparator", "."),
            CurrencyFormat = GetValue(dict, "CurrencyFormat", "SymbolFirst"),

            // Theme Settings
            ThemeMode = GetValue(dict, "ThemeMode", "light"),
            PrimaryColor = GetValue(dict, "PrimaryColor", "#0d5c1f"),
            AccentColor = GetValue(dict, "AccentColor", "#22c55e"),
            SidebarColor = GetValue(dict, "SidebarColor", "#0d5c1f")
        };
    }

    public async Task<bool> SaveSettingsAsync(int organizationId, int? storeId, SettingsDto model, int userId)
    {
        try
        {
            var settings = await _unitOfWork.Settings
                .FindAsync(s => s.OrganizationId == organizationId && (s.StoreId == storeId || s.StoreId == null));

            var dict = settings.ToDictionary(s => s.Key, s => s);

            // Helper to save settings
            async Task SaveSettingAsync(string key, string value, string group)
            {
                if (dict.TryGetValue(key, out var setting))
                {
                    setting.Value = value;
                    setting.UpdatedAt = DateTime.UtcNow;
                    setting.UpdatedBy = userId;
                    _unitOfWork.Settings.Update(setting);
                }
                else
                {
                    setting = new Setting
                    {
                        OrganizationId = organizationId,
                        StoreId = storeId,
                        Key = key,
                        Value = value,
                        Group = group,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedBy = userId
                    };
                    await _unitOfWork.Settings.AddAsync(setting);
                }
            }

            // Save Store Settings
            SaveSettingAsync("StoreName", model.StoreName, "Store");
            SaveSettingAsync("StoreCode", model.StoreCode ?? "", "Store");
            SaveSettingAsync("StoreAddress", model.StoreAddress ?? "", "Store");
            SaveSettingAsync("StorePhone", model.StorePhone ?? "", "Store");
            SaveSettingAsync("StoreEmail", model.StoreEmail ?? "", "Store");
            SaveSettingAsync("StoreLogo", model.StoreLogo ?? "", "Store");

            // Save Company Settings
            SaveSettingAsync("CompanyName", model.CompanyName ?? "", "Company");
            SaveSettingAsync("RegistrationNumber", model.RegistrationNumber ?? "", "Company");
            SaveSettingAsync("VatNumber", model.VatNumber ?? "", "Company");
            SaveSettingAsync("CompanyAddress", model.CompanyAddress ?? "", "Company");

            // Save Tax Settings
            SaveSettingAsync("VatRate", model.VatRate.ToString(), "Tax");
            SaveSettingAsync("TaxMethod", model.TaxMethod, "Tax");
            SaveSettingAsync("TaxNumberLabel", model.TaxNumberLabel ?? "VAT Number", "Tax");

            // Save Receipt Settings
            SaveSettingAsync("ReceiptHeader", model.ReceiptHeader ?? "", "Receipt");
            SaveSettingAsync("ReceiptFooter", model.ReceiptFooter ?? "", "Receipt");
            SaveSettingAsync("ShowLogoOnReceipt", model.ShowLogoOnReceipt.ToString(), "Receipt");
            SaveSettingAsync("ReceiptPaperSize", model.ReceiptPaperSize, "Receipt");
            SaveSettingAsync("ShowBarcodeOnReceipt", model.ShowBarcodeOnReceipt.ToString(), "Receipt");

            // Save Currency Settings
            SaveSettingAsync("CurrencySymbol", model.CurrencySymbol, "Currency");
            SaveSettingAsync("CurrencyCode", model.CurrencyCode, "Currency");
            SaveSettingAsync("DecimalPlaces", model.DecimalPlaces.ToString(), "Currency");
            SaveSettingAsync("ThousandsSeparator", model.ThousandsSeparator, "Currency");
            SaveSettingAsync("DecimalSeparator", model.DecimalSeparator, "Currency");
            SaveSettingAsync("CurrencyFormat", model.CurrencyFormat, "Currency");

            // Save Theme Settings
            SaveSettingAsync("ThemeMode", model.ThemeMode, "Theme");
            SaveSettingAsync("PrimaryColor", model.PrimaryColor, "Theme");
            SaveSettingAsync("AccentColor", model.AccentColor, "Theme");
            SaveSettingAsync("SidebarColor", model.SidebarColor, "Theme");

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<T?> GetSettingValueAsync<T>(int organizationId, string key, int? storeId = null)
    {
        var value = await GetSettingValueAsync(organizationId, key, storeId);
        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return default;
        }
    }

    public async Task<string?> GetSettingValueAsync(int organizationId, string key, int? storeId = null)
    {
        var setting = await _unitOfWork.Settings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId &&
                                     s.Key == key &&
                                     (s.StoreId == storeId || s.StoreId == null));

        return setting?.Value;
    }

    public async Task<bool> SetSettingValueAsync(int organizationId, string key, string value, int userId, int? storeId = null, string? group = null)
    {
        try
        {
            var setting = await _unitOfWork.Settings
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId &&
                                         s.Key == key &&
                                         (s.StoreId == storeId || s.StoreId == null));

            if (setting != null)
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedBy = userId;
                _unitOfWork.Settings.Update(setting);
            }
            else
            {
                setting = new Setting
                {
                    OrganizationId = organizationId,
                    StoreId = storeId,
                    Key = key,
                    Value = value,
                    Group = group ?? "General",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                await _unitOfWork.Settings.AddAsync(setting);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetValue(Dictionary<string, string> dict, string key, string defaultValue)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private T GetValue<T>(Dictionary<string, string> dict, string key, T defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}