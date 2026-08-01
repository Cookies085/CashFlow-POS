using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CashFlow.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public AuthService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
    {
        try
        {
            // Find user by username
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
                u.Username == username && u.IsActive);

            if (user == null)
            {
                return (false, "Invalid username or password.", null);
            }

            // Verify password
            if (!VerifyPassword(password, user.PasswordHash))
            {
                return (false, "Invalid username or password.", null);
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return (true, "Login successful.", user);
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, User? User, Organization? Organization)> RegisterAsync(
    string username,
    string fullName,
    string email,
    string password,
    string organizationName,
    string? phone = null,
    string? address = null)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                return (false, "Username already exists.", null, null);
            }

            // Check if organization name already exists
            var existingOrg = await _unitOfWork.Organizations.FirstOrDefaultAsync(o => o.Name == organizationName);
            if (existingOrg != null)
            {
                return (false, "Organization name already exists.", null, null);
            }

            // Create organization
            var organization = new Organization
            {
                Name = organizationName,
                Phone = phone,
                Address = address,
                Email = email,
                IsActive = true,
                SubscriptionPlan = SubscriptionPlan.Basic,
                SubscriptionExpiry = DateTime.UtcNow.AddMonths(1), // 1 month trial
                CreatedAt = DateTime.UtcNow,
                MaxUsers = 5,
                MaxStores = 1,
                MaxProducts = 500
            };

            await _unitOfWork.Organizations.AddAsync(organization);
            await _unitOfWork.SaveChangesAsync();

            // Create the user (SuperAdmin of the organization)
            var user = new User
            {
                OrganizationId = organization.Id,
                Username = username,
                FullName = fullName,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = UserRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = null // Self-registered
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate store code
            var storeCode = GenerateStoreCode(organizationName);

            // Create default store for the organization with store code
            var store = new Store
            {
                OrganizationId = organization.Id,
                Name = "Main Store",
                Address = address,
                Phone = phone,
                Email = email,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Id,
                StoreCode = storeCode,
                ThemeColor = "#0d5c1f",
                ThemeMode = "light",
                Timezone = "Africa/Johannesburg"
            };

            await _unitOfWork.Stores.AddAsync(store);
            await _unitOfWork.SaveChangesAsync();

            return (true, $"Registration successful! Your store code is: {storeCode}", user, organization);
        }
        catch (Exception ex)
        {
            return (false, $"An error occurred: {ex.Message}", null, null);
        }
    }

    // Add this helper method to AuthService.cs
    private string GenerateStoreCode(string organizationName)
    {
        // Take first 3 letters of organization name + random 3 digits
        var prefix = organizationName.Length >= 3 ?
            organizationName.Substring(0, 3).ToUpper() :
            organizationName.ToUpper().PadRight(3, 'X');
        var random = new Random().Next(100, 999);
        return $"{prefix}{random}";
    }

    public async Task<bool> LogoutAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Just return true, session handling will be done by the controller
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _unitOfWork.Users.GetByIdAsync(id);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return false;

            // Verify current password
            if (!VerifyPassword(currentPassword, user.PasswordHash))
                return false;

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }
}