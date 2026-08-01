using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.Filters;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Web.Controllers;

[AuthorizeRole(UserRole.SuperAdmin, UserRole.Admin, UserRole.Manager, UserRole.Cashier)]
public class ShiftsController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public ShiftsController(IUnitOfWork unitOfWork, IAuditService auditService) : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: Shifts
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, DateTime? dateFrom, DateTime? dateTo, int page = 1)
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");
        var currentUserRole = HttpContext.Session.GetString("UserRole");
        var currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;

        // Get all shifts for the organization with User and Store included
        var shifts = await _unitOfWork.Shifts
            .FindWithIncludeAsync(
                s => s.OrganizationId == organizationId,
                s => s.User,
                s => s.Store
            );

        // Filter by store (if not SuperAdmin or Admin)
        if (storeId.HasValue && currentUserRole != "SuperAdmin" && currentUserRole != "Admin")
        {
            shifts = shifts.Where(s => s.StoreId == storeId.Value);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            shifts = shifts.Where(s =>
                (s.User != null && s.User.FullName != null && s.User.FullName.ToLower().Contains(searchTerm)) ||
                (s.Store != null && s.Store.Name != null && s.Store.Name.ToLower().Contains(searchTerm)));
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(statusFilter))
        {
            shifts = shifts.Where(s => s.Status == statusFilter);
        }

        // Apply date filters
        if (dateFrom.HasValue)
        {
            shifts = shifts.Where(s => s.StartTime.Date >= dateFrom.Value.Date);
        }
        if (dateTo.HasValue)
        {
            shifts = shifts.Where(s => s.StartTime.Date <= dateTo.Value.Date);
        }

        // Order by start time descending
        shifts = shifts.OrderByDescending(s => s.StartTime);

        // Convert to ViewModel with null checks
        var shiftViewModels = shifts.Select(s => new ShiftViewModel
        {
            Id = s.Id,
            CashierName = s.User?.FullName ?? "Unknown",
            StoreName = s.Store?.Name ?? "Main Store",
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            OpeningBalance = s.OpeningBalance,
            ClosingBalance = s.ClosingBalance,
            ExpectedBalance = s.ExpectedBalance,
            Difference = s.Difference,
            Status = s.Status,
            Notes = s.Notes,
            Duration = s.EndTime.HasValue ?
                $"{Math.Floor((s.EndTime.Value - s.StartTime).TotalHours)}h {(s.EndTime.Value - s.StartTime).Minutes}m" :
                "In Progress"
        }).ToList();

        // Calculate totals for each shift (sales during shift)
        foreach (var shiftVM in shiftViewModels)
        {
            var shiftEntity = shifts.First(s => s.Id == shiftVM.Id);

            // Get sales during the shift
            var sales = await _unitOfWork.Sales
                .FindAsync(s => s.CreatedAt >= shiftEntity.StartTime &&
                               s.CreatedAt <= (shiftEntity.EndTime ?? DateTime.UtcNow) &&
                               s.UserId == shiftEntity.UserId &&
                               s.PaymentStatus == "Paid");

            shiftVM.TotalOrders = sales.Count();
            shiftVM.TotalSales = sales.Sum(s => s.NetAmount);
            shiftVM.TotalCashSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.NetAmount);
            shiftVM.TotalCardSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.NetAmount);
            shiftVM.TotalMobileSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Mobile).Sum(s => s.NetAmount);
            shiftVM.TotalCreditSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Credit).Sum(s => s.NetAmount);
        }

        // Pagination
        int pageSize = 10;
        int totalCount = shiftViewModels.Count;
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedShifts = shiftViewModels
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var model = new ShiftListViewModel
        {
            Shifts = pagedShifts,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            DateFrom = dateFrom,
            DateTo = dateTo,
            TotalSalesAllShifts = shiftViewModels.Sum(s => s.TotalSales)
        };

        ViewBag.Statuses = new List<string> { "Open", "Closed" };

        // Log view shifts
        await LogAuditAsync(
            "View",
            "Shifts",
            null,
            null,
            null,
            $"Viewed shifts list. Page: {page}, Total shifts: {totalCount}",
            null);

        return View(model);
    }

    // GET: Shifts/Start
    public async Task<IActionResult> Start()
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        // Check if user already has an open shift
        var openShift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Open");

        var model = new StartShiftViewModel
        {
            HasOpenShift = openShift != null
        };

        if (openShift != null)
        {
            model.OpenShift = new ShiftViewModel
            {
                Id = openShift.Id,
                StartTime = openShift.StartTime,
                OpeningBalance = openShift.OpeningBalance,
                Status = openShift.Status,
                CashierName = (await _unitOfWork.Users.GetByIdAsync(userId))?.FullName ?? "Unknown"
            };
        }

        // Log view start shift page
        await LogAuditAsync(
            "View",
            "ShiftStart",
            null,
            null,
            null,
            $"Viewed start shift page. Has open shift: {openShift != null}",
            null);

        return View(model);
    }

    // POST: Shifts/Start
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(StartShiftViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Check if user already has an open shift
        var openShift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Open");

        if (openShift != null)
        {
            TempData["ErrorMessage"] = "You already have an open shift. Please close it first.";
            return RedirectToAction("Start");
        }

        var shift = new Shift
        {
            OrganizationId = organizationId,
            UserId = userId,
            StoreId = storeId,
            StartTime = DateTime.UtcNow,
            OpeningBalance = model.OpeningBalance,
            Status = "Open",
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _unitOfWork.Shifts.AddAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        // Store shift ID in session
        HttpContext.Session.SetInt32("CurrentShiftId", shift.Id);

        // Log audit
        await LogAuditAsync(
            "Start",
            "Shift",
            shift.Id,
            null,
            shift,
            $"Started shift. Opening balance: R {model.OpeningBalance:N2}",
            $"Shift #{shift.Id}");

        TempData["SuccessMessage"] = $"Shift started successfully! Opening balance: R {model.OpeningBalance:N2}";
        return RedirectToAction("Index", "Home");
    }

    // GET: Shifts/End
    public async Task<IActionResult> End()
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

        // Find open shift
        var shift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Open");

        if (shift == null)
        {
            TempData["ErrorMessage"] = "You don't have an open shift to close.";
            return RedirectToAction("Index");
        }

        // Get sales during the shift
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.CreatedAt >= shift.StartTime &&
                           s.UserId == userId &&
                           s.PaymentStatus == "Paid");

        var model = new EndShiftViewModel
        {
            ShiftId = shift.Id,
            Shift = new ShiftViewModel
            {
                Id = shift.Id,
                StartTime = shift.StartTime,
                OpeningBalance = shift.OpeningBalance,
                CashierName = (await _unitOfWork.Users.GetByIdAsync(userId))?.FullName ?? "Unknown"
            },
            TotalOrders = sales.Count(),
            TotalSales = sales.Sum(s => s.NetAmount),
            TotalCashSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.NetAmount),
            TotalCardSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.NetAmount),
            TotalMobileSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Mobile).Sum(s => s.NetAmount),
            TotalCreditSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Credit).Sum(s => s.NetAmount)
        };

        // Log view end shift page
        await LogAuditAsync(
            "View",
            "ShiftEnd",
            shift.Id,
            null,
            null,
            $"Viewed end shift page. Shift started: {shift.StartTime}. Total sales: {model.TotalSales:C}",
            $"Shift #{shift.Id}");

        return View(model);
    }

    // POST: Shifts/End
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> End(EndShiftViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

        var shift = await _unitOfWork.Shifts.GetByIdAsync(model.ShiftId);
        if (shift == null)
        {
            return NotFound();
        }

        // Get sales during the shift
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.CreatedAt >= shift.StartTime &&
                           s.UserId == userId &&
                           s.PaymentStatus == "Paid");

        var totalSales = sales.Sum(s => s.NetAmount);
        var totalCashSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.NetAmount);

        // Expected balance = Opening balance + Cash sales
        var expectedBalance = shift.OpeningBalance + totalCashSales;
        var difference = model.ClosingBalance - expectedBalance;

        // Save old values for audit
        var oldShift = new
        {
            shift.Status,
            shift.OpeningBalance,
            shift.ExpectedBalance,
            shift.ClosingBalance,
            shift.Difference
        };

        // Update shift
        shift.EndTime = DateTime.UtcNow;
        shift.ClosingBalance = model.ClosingBalance;
        shift.ExpectedBalance = expectedBalance;
        shift.Difference = difference;
        shift.Status = "Closed";
        shift.Notes = model.Notes ?? shift.Notes;

        _unitOfWork.Shifts.Update(shift);
        await _unitOfWork.SaveChangesAsync();

        // Clear shift ID from session
        HttpContext.Session.Remove("CurrentShiftId");

        var status = difference >= 0 ? "over" : "short";

        // Log audit
        await LogAuditAsync(
            "Close",
            "Shift",
            shift.Id,
            oldShift,
            shift,
            $"Closed shift. Expected: R {expectedBalance:N2}, Actual: R {model.ClosingBalance:N2}, Difference: R {Math.Abs(difference):N2} ({status})",
            $"Shift #{shift.Id}");

        TempData["SuccessMessage"] = $"Shift closed successfully! " +
            $"Expected: R {expectedBalance:N2}, " +
            $"Actual: R {model.ClosingBalance:N2}, " +
            $"Difference: R {Math.Abs(difference):N2} ({status})";

        return RedirectToAction(nameof(Index));
    }

    // GET: Shifts/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var shift = await _unitOfWork.Shifts.GetByIdAsync(id);
        if (shift == null)
        {
            return NotFound();
        }

        // Get sales during the shift
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.CreatedAt >= shift.StartTime &&
                           s.CreatedAt <= (shift.EndTime ?? DateTime.UtcNow) &&
                           s.UserId == shift.UserId &&
                           s.PaymentStatus == "Paid");

        var model = new ShiftViewModel
        {
            Id = shift.Id,
            CashierName = (await _unitOfWork.Users.GetByIdAsync(shift.UserId))?.FullName ?? "Unknown",
            StoreName = shift.Store?.Name ?? "Main Store",
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            OpeningBalance = shift.OpeningBalance,
            ClosingBalance = shift.ClosingBalance,
            ExpectedBalance = shift.ExpectedBalance,
            Difference = shift.Difference,
            Status = shift.Status,
            Notes = shift.Notes,
            TotalOrders = sales.Count(),
            TotalSales = sales.Sum(s => s.NetAmount),
            TotalCashSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.NetAmount),
            TotalCardSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.NetAmount),
            TotalMobileSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Mobile).Sum(s => s.NetAmount),
            TotalCreditSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Credit).Sum(s => s.NetAmount),
            Duration = shift.EndTime.HasValue ?
                $"{Math.Floor((shift.EndTime.Value - shift.StartTime).TotalHours)}h {(shift.EndTime.Value - shift.StartTime).Minutes}m" :
                "In Progress"
        };

        ViewBag.Sales = sales.OrderByDescending(s => s.CreatedAt).Take(50).ToList();

        // Log view shift details
        await LogAuditAsync(
            "View",
            "ShiftDetails",
            shift.Id,
            null,
            shift,
            $"Viewed shift details. Status: {shift.Status}, Sales: {model.TotalSales:C}",
            $"Shift #{shift.Id}");

        return View(model);
    }

    // GET: Shifts/Current
    public async Task<IActionResult> Current()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Json(new { hasOpenShift = false });
        }

        var shift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Status == "Open");

        if (shift == null)
        {
            return Json(new { hasOpenShift = false });
        }

        // Get sales during the shift
        var sales = await _unitOfWork.Sales
            .FindAsync(s => s.CreatedAt >= shift.StartTime &&
                           s.UserId == userId.Value &&
                           s.PaymentStatus == "Paid");

        return Json(new
        {
            hasOpenShift = true,
            shiftId = shift.Id,
            startTime = shift.StartTime,
            openingBalance = shift.OpeningBalance,
            totalOrders = sales.Count(),
            totalSales = sales.Sum(s => s.NetAmount),
            totalCashSales = sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.NetAmount),
            cashierName = (await _unitOfWork.Users.GetByIdAsync(userId.Value))?.FullName ?? "Unknown"
        });
    }

    // GET: Shifts/CheckStatus
    public async Task<IActionResult> CheckStatus()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Json(new { hasShift = false, isActive = false });
        }

        var openShift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Status == "Open");

        if (openShift != null)
        {
            // User has an open shift
            return Json(new
            {
                hasShift = true,
                isActive = true,
                shiftId = openShift.Id,
                startTime = openShift.StartTime,
                openingBalance = openShift.OpeningBalance
            });
        }

        // Check if user has ever had a shift (to distinguish new from returning)
        var anyShift = await _unitOfWork.Shifts
            .AnyAsync(s => s.UserId == userId.Value);

        return Json(new
        {
            hasShift = false,
            isActive = false,
            hasPreviousShifts = anyShift
        });
    }

    // GET: Shifts/Prompt
    public async Task<IActionResult> Prompt()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        // Check if user already has an open shift
        var openShift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Status == "Open");

        if (openShift != null)
        {
            // Already has a shift, redirect to dashboard
            return RedirectToAction("Index", "Home");
        }

        // Log view shift prompt
        await LogAuditAsync(
            "View",
            "ShiftPrompt",
            null,
            null,
            null,
            $"Viewed shift prompt page for user: {userId.Value}",
            null);

        return View();
    }

    // POST: Shifts/StartShiftFromPrompt
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartShiftFromPrompt()
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "User not logged in";
                return RedirectToAction("Login", "Auth");
            }

            // Check if user already has an open shift
            var existingShift = await _unitOfWork.Shifts
                .FirstOrDefaultAsync(s => s.UserId == userId.Value && s.Status == "Open");

            if (existingShift != null)
            {
                TempData["SuccessMessage"] = "Shift already active";
                return RedirectToAction("Index", "Home");
            }

            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
            var storeId = HttpContext.Session.GetInt32("StoreId");

            // Create a new shift with default opening balance (0)
            var shift = new Shift
            {
                OrganizationId = organizationId,
                UserId = userId.Value,
                StoreId = storeId,
                StartTime = DateTime.UtcNow,
                OpeningBalance = 0,
                Status = "Open",
                Notes = "Auto-started from login prompt",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId.Value
            };

            await _unitOfWork.Shifts.AddAsync(shift);
            await _unitOfWork.SaveChangesAsync();

            // Store shift ID in session
            HttpContext.Session.SetInt32("CurrentShiftId", shift.Id);

            // Log audit
            await LogAuditAsync(
                "Start",
                "Shift",
                shift.Id,
                null,
                shift,
                "Auto-started shift from login prompt",
                $"Shift #{shift.Id}");

            TempData["SuccessMessage"] = "Shift started successfully!";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Failed to start shift: {ex.Message}";
            return RedirectToAction("Prompt");
        }
    }

    // POST: Shifts/SetSkipPreference
    [HttpPost]
    public async Task<IActionResult> SetSkipPreference(bool skip)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue)
        {
            return Json(new { success = false });
        }

        // Store preference in session
        HttpContext.Session.SetString("SkipShiftPrompt", skip.ToString());

        // Log skip preference
        await LogAuditAsync(
            "Preference",
            "Shift",
            null,
            null,
            new { SkipShiftPrompt = skip },
            $"User set shift skip preference to: {skip}",
            null);

        return Json(new { success = true });
    }
}