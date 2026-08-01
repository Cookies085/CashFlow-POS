using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace CashFlow.Web.Controllers;

public class UsersController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public UsersController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Users
    public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        // Check if user has permission (only SuperAdmin and Admin can manage users)
        var currentUserRole = HttpContext.Session.GetString("UserRole");
        if (currentUserRole != "SuperAdmin" && currentUserRole != "Admin")
        {
            TempData["ErrorMessage"] = "You don't have permission to manage users.";
            return RedirectToAction("Index", "Home");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get all users for the organization
        var users = await _unitOfWork.Users
            .FindAsync(u => u.OrganizationId == organizationId);

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            users = users.Where(u =>
                u.Username.ToLower().Contains(searchTerm) ||
                u.FullName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm));
        }

        // Apply role filter
        if (!string.IsNullOrEmpty(roleFilter) && Enum.TryParse<UserRole>(roleFilter, out var role))
        {
            users = users.Where(u => u.Role == role);
        }

        // Order by creation date
        users = users.OrderByDescending(u => u.CreatedAt);

        // Get creator names
        var creatorIds = users.Where(u => u.CreatedBy.HasValue).Select(u => u.CreatedBy.Value).Distinct();
        var creators = new Dictionary<int, string>();
        foreach (var id in creatorIds)
        {
            var creator = await _unitOfWork.Users.GetByIdAsync(id);
            if (creator != null)
            {
                creators[id] = creator.FullName;
            }
        }

        // Convert to ViewModel
        var userViewModels = users.Select(u => new UserViewModel
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            StoreId = u.StoreId,
            StoreName = u.Store?.Name,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            CreatedBy = u.CreatedBy.HasValue ? creators.GetValueOrDefault(u.CreatedBy.Value, "Unknown") : "System"
        }).ToList();

        // Pagination
        int pageSize = 10;
        int totalCount = userViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedUsers = userViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new UserListViewModel
        {
            Users = pagedUsers,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            RoleFilter = roleFilter
        };

        // Get roles for filter
        var roles = Enum.GetValues(typeof(UserRole))
            .Cast<UserRole>()
            .Select(r => r.ToString())
            .ToList();

        ViewBag.Roles = roles;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentRole = roleFilter;

        // Log view users action
        await LogAuditAsync(
            "View",
            "Users",
            null,
            null,
            null,
            $"Viewed users list. Page: {page}, Total users: {totalCount}",
            null);

        return View(model);
    }

    // GET: Users/Create
    public async Task<IActionResult> Create()
    {
        // Check if user has permission
        var currentUserRole = HttpContext.Session.GetString("UserRole");
        if (currentUserRole != "SuperAdmin" && currentUserRole != "Admin")
        {
            TempData["ErrorMessage"] = "You don't have permission to create users.";
            return RedirectToAction("Index", "Home");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Get stores for dropdown
        var stores = await _unitOfWork.Stores
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        ViewBag.Stores = stores.OrderBy(s => s.Name).ToList();

        return View(new UserViewModel());
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

                // Check if username already exists
                var existingUser = await _unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Username == model.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    await PopulateStoresDropdown();
                    return View(model);
                }

                // Check if email already exists
                var existingEmail = await _unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.OrganizationId == organizationId && u.Email == model.Email);

                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    await PopulateStoresDropdown();
                    return View(model);
                }

                // Hash password
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

                var user = new User
                {
                    OrganizationId = organizationId,
                    Username = model.Username,
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    Role = model.Role,
                    StoreId = model.StoreId,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(
                    "Create",
                    "User",
                    user.Id,
                    null,
                    user,
                    $"Created user: {user.FullName} ({user.Username}) with role: {user.Role}",
                    user.Username);

                TempData["SuccessMessage"] = $"User {user.FullName} created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
        }

        await PopulateStoresDropdown();
        return View(model);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        // Check if user has permission
        var currentUserRole = HttpContext.Session.GetString("UserRole");
        if (currentUserRole != "SuperAdmin" && currentUserRole != "Admin")
        {
            TempData["ErrorMessage"] = "You don't have permission to edit users.";
            return RedirectToAction("Index", "Home");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Prevent editing SuperAdmin if not SuperAdmin
        if (user.Role == UserRole.SuperAdmin && currentUserRole != "SuperAdmin")
        {
            TempData["ErrorMessage"] = "You cannot edit a SuperAdmin user.";
            return RedirectToAction(nameof(Index));
        }

        var model = new UserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            StoreId = user.StoreId,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };

        await PopulateStoresDropdown();
        return View(model);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
                var currentUserRole = HttpContext.Session.GetString("UserRole");

                // Prevent editing SuperAdmin if not SuperAdmin
                if (user.Role == UserRole.SuperAdmin && currentUserRole != "SuperAdmin")
                {
                    TempData["ErrorMessage"] = "You cannot edit a SuperAdmin user.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if username already exists (excluding current user)
                var existingUser = await _unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.OrganizationId == organizationId &&
                                             u.Username == model.Username &&
                                             u.Id != id);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    await PopulateStoresDropdown();
                    return View(model);
                }

                // Check if email already exists (excluding current user)
                var existingEmail = await _unitOfWork.Users
                    .FirstOrDefaultAsync(u => u.OrganizationId == organizationId &&
                                             u.Email == model.Email &&
                                             u.Id != id);

                if (existingEmail != null)
                {
                    ModelState.AddModelError("Email", "Email already exists.");
                    await PopulateStoresDropdown();
                    return View(model);
                }

                // Save old values for audit
                var oldUser = new
                {
                    user.Username,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.StoreId,
                    user.IsActive
                };

                user.Username = model.Username;
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Role = model.Role;
                user.StoreId = model.StoreId;
                user.IsActive = model.IsActive;

                // Update password if provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
                }

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(
                    "Edit",
                    "User",
                    user.Id,
                    oldUser,
                    user,
                    $"Updated user: {user.FullName} ({user.Username}). Role: {user.Role}, Active: {user.IsActive}",
                    user.Username);

                TempData["SuccessMessage"] = $"User {user.FullName} updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
            }
        }

        await PopulateStoresDropdown();
        return View(model);
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        // Check if user has permission
        var currentUserRole = HttpContext.Session.GetString("UserRole");
        if (currentUserRole != "SuperAdmin" && currentUserRole != "Admin")
        {
            TempData["ErrorMessage"] = "You don't have permission to delete users.";
            return RedirectToAction("Index", "Home");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Prevent deleting self
        var currentUserId = HttpContext.Session.GetInt32("UserId");
        if (currentUserId.HasValue && currentUserId.Value == id)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Index));
        }

        // Prevent deleting SuperAdmin if not SuperAdmin
        if (user.Role == UserRole.SuperAdmin && currentUserRole != "SuperAdmin")
        {
            TempData["ErrorMessage"] = "You cannot delete a SuperAdmin user.";
            return RedirectToAction(nameof(Index));
        }

        var model = new UserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        };

        return View(model);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user != null)
        {
            // Prevent deleting self
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            // Save old values for audit
            var oldUser = new
            {
                user.Username,
                user.FullName,
                user.Role,
                user.IsActive
            };

            // Soft delete - just deactivate
            user.IsActive = false;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(
                "Delete",
                "User",
                user.Id,
                oldUser,
                null,
                $"Deactivated user: {user.FullName} ({user.Username})",
                user.Username);

            TempData["SuccessMessage"] = $"User {user.FullName} has been deactivated.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Users/Activate/5
    public async Task<IActionResult> Activate(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user != null)
        {
            user.IsActive = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // Log audit
            await LogAuditAsync(
                "Activate",
                "User",
                user.Id,
                null,
                user,
                $"Activated user: {user.FullName} ({user.Username})",
                user.Username);

            TempData["SuccessMessage"] = $"User {user.FullName} has been activated.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateStoresDropdown()
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var stores = await _unitOfWork.Stores
            .FindAsync(s => s.OrganizationId == organizationId && s.IsActive);

        ViewBag.Stores = stores.OrderBy(s => s.Name).ToList();
    }
}