using CashFlow.Core.Entities;

namespace CashFlow.Core.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password);
    Task<(bool Success, string Message, User? User, Organization? Organization)> RegisterAsync(
        string username,
        string fullName,
        string email,
        string password,
        string organizationName,
        string? phone = null,
        string? address = null);
    Task<bool> LogoutAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(int id);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    bool VerifyPassword(string password, string passwordHash);
    string HashPassword(string password);
}