using CashFlow.Core.Entities;
using CashFlow.Core.Enums;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Services;
using CashFlow.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CashFlow.Web.Controllers;

public class POSController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;

    public POSController(IUnitOfWork unitOfWork, IAuditService auditService)
        : base(auditService)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: POS
    public async Task<IActionResult> Index()
    {
        // Check if user is logged in
        if (!HttpContext.Session.GetInt32("UserId").HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        // Get active products for the store
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId &&
                           p.IsActive &&
                           (p.StoreId == storeId || p.StoreId == null));

        var model = new POSViewModel
        {
            CartItems = new List<CartItemViewModel>(),
            Subtotal = 0,
            TaxAmount = 0,
            DiscountAmount = 0,
            TotalAmount = 0
        };

        ViewBag.Products = products.OrderBy(p => p.ItemName).ToList();
        ViewBag.StoreId = storeId;

        // Log POS page view
        await LogAuditAsync(
            "View",
            "POS",
            null,
            null,
            null,
            "Viewed POS page",
            null);

        return View(model);
    }

    // POST: POS/AddToCart
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null || !product.IsActive)
            {
                return Json(new { success = false, message = "Product not found or inactive" });
            }

            // Get current cart from session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

            // Check if product already in cart
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.Subtotal = existingItem.UnitPrice * existingItem.Quantity;
                existingItem.TaxAmount = CalculateVATAmount(existingItem.Subtotal, existingItem.TaxRate);
            }
            else
            {
                var subtotal = product.SellingPrice * quantity;
                var taxAmount = CalculateVATAmount(subtotal, product.TaxRate);

                cart.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ItemCode = product.ItemCode,
                    ItemName = product.ItemName,
                    Size = product.Size,
                    Quantity = quantity,
                    UnitPrice = product.SellingPrice,
                    Discount = 0,
                    TaxRate = product.TaxRate,
                    Subtotal = subtotal,
                    TaxAmount = taxAmount
                });
            }

            // Save cart to session
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            // Calculate totals
            var totals = CalculateTotals(cart);

            return Json(new
            {
                success = true,
                cart = cart,
                subtotal = totals.Subtotal,
                taxAmount = totals.TaxAmount,
                totalAmount = totals.TotalAmount,
                itemCount = cart.Sum(c => c.Quantity)
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddToCart Error: {ex.Message}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // POST: POS/UpdateCart
    [HttpPost]
    public async Task<IActionResult> UpdateCart(int productId, int quantity)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.Subtotal = item.UnitPrice * quantity;
                item.TaxAmount = CalculateVATAmount(item.Subtotal, item.TaxRate);
            }
        }

        HttpContext.Session.SetObjectAsJson("Cart", cart);

        var totals = CalculateTotals(cart);

        return Json(new
        {
            success = true,
            cart = cart,
            subtotal = totals.Subtotal,
            taxAmount = totals.TaxAmount,
            totalAmount = totals.TotalAmount,
            itemCount = cart.Sum(c => c.Quantity)
        });
    }

    // Helper method to calculate VAT
    private decimal CalculateVATAmount(decimal amount, decimal taxRate)
    {
        // VAT is calculated as: amount * (taxRate / (100 + taxRate))
        // This gives the VAT portion when the amount INCLUDES VAT
        if (taxRate <= 0) return 0;
        return amount * (taxRate / (100 + taxRate));
    }

    // Helper methods
    private (decimal Subtotal, decimal TaxAmount, decimal TotalAmount) CalculateTotals(
        List<CartItemViewModel> cart,
        decimal discountAmount = 0)
    {
        // Subtotal is the total price including VAT for all items
        var subtotal = cart.Sum(c => c.Subtotal);

        // TaxAmount is the total VAT portion
        var taxAmount = cart.Sum(c => c.TaxAmount);

        // Total = Subtotal - Discount (Subtotal already includes VAT)
        var total = subtotal - discountAmount;

        return (subtotal, taxAmount, total);
    }

    // POST: POS/RemoveFromCart
    [HttpPost]
    public IActionResult RemoveFromCart(int productId)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

        cart.RemoveAll(c => c.ProductId == productId);
        HttpContext.Session.SetObjectAsJson("Cart", cart);

        var totals = CalculateTotals(cart);

        return Json(new
        {
            success = true,
            cart = cart,
            subtotal = totals.Subtotal,
            taxAmount = totals.TaxAmount,
            totalAmount = totals.TotalAmount,
            itemCount = cart.Sum(c => c.Quantity)
        });
    }

    // POST: POS/ClearCart
    [HttpPost]
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove("Cart");
        return Json(new { success = true });
    }

    // GET: POS/SearchProducts
    [HttpGet]
    public async Task<IActionResult> SearchProducts(string term)
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
        var storeId = HttpContext.Session.GetInt32("StoreId");

        if (string.IsNullOrEmpty(term) || term.Length < 2)
        {
            return Json(new List<object>());
        }

        term = term.ToLower();
        var products = await _unitOfWork.Products
            .FindAsync(p => p.OrganizationId == organizationId &&
                           p.IsActive &&
                           (p.StoreId == storeId || p.StoreId == null) &&
                           (p.ItemCode.ToLower().Contains(term) ||
                            p.ItemName.ToLower().Contains(term) ||
                            (p.Barcode != null && p.Barcode.ToLower().Contains(term))));

        var result = products.Select(p => new
        {
            id = p.Id,
            itemCode = p.ItemCode,
            itemName = p.ItemName,
            size = p.Size,
            sellingPrice = p.SellingPrice,
            currentStock = p.CurrentStock,
            taxRate = p.TaxRate,
            isInStock = p.CurrentStock > 0
        }).Take(20).ToList();

        // Log product search
        await LogAuditAsync(
            "Search",
            "Products",
            null,
            null,
            new { SearchTerm = term, Results = result.Count() },
            $"Searched products with term: '{term}'. Found {result.Count()} results",
            null);

        return Json(result);
    }

    // GET: POS/GetCustomer
    [HttpGet]
    public async Task<IActionResult> GetCustomer(string phone)
    {
        var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;

        if (string.IsNullOrEmpty(phone))
        {
            return Json(new { success = false });
        }

        var customer = await _unitOfWork.Customers
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Phone == phone && c.IsActive);

        if (customer == null)
        {
            return Json(new { success = false, message = "Customer not found" });
        }

        // Log customer lookup
        await LogAuditAsync(
            "Lookup",
            "Customer",
            customer.Id,
            null,
            customer,
            $"Customer lookup by phone: {phone}. Found: {customer.FirstName} {customer.LastName}",
            customer.Phone);

        return Json(new
        {
            success = true,
            customer = new
            {
                id = customer.Id,
                firstName = customer.FirstName,
                lastName = customer.LastName,
                phone = customer.Phone,
                email = customer.Email,
                loyaltyPoints = customer.LoyaltyPoints,
                pointsTier = customer.PointsTier,
                totalSpent = customer.TotalSpent
            }
        });
    }

    // POST: POS/ApplyDiscount
    [HttpPost]
    public IActionResult ApplyDiscount(decimal discountAmount)
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

        if (discountAmount < 0 || discountAmount > CalculateTotals(cart).Subtotal)
        {
            return Json(new { success = false, message = "Invalid discount amount" });
        }

        HttpContext.Session.SetDecimal("DiscountAmount", discountAmount);

        var totals = CalculateTotals(cart, discountAmount);

        // Log discount applied
        LogAuditAsync(
            "Discount",
            "Cart",
            null,
            null,
            new { DiscountAmount = discountAmount, CartTotal = totals.TotalAmount },
            $"Applied discount: R {discountAmount:F2}. New total: R {totals.TotalAmount:F2}",
            null).Wait();

        return Json(new
        {
            success = true,
            subtotal = totals.Subtotal,
            taxAmount = totals.TaxAmount,
            discountAmount = discountAmount,
            totalAmount = totals.TotalAmount
        });
    }

    // POST: POS/ProcessSale
    [HttpPost]
    public async Task<IActionResult> ProcessSale([FromBody] POSViewModel model)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var organizationId = HttpContext.Session.GetInt32("OrganizationId") ?? 0;
            var storeId = HttpContext.Session.GetInt32("StoreId");

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

            if (!cart.Any())
            {
                return Json(new { success = false, message = "Cart is empty" });
            }

            // Calculate totals
            var discountAmount = HttpContext.Session.GetDecimal("DiscountAmount") ?? 0;
            var totals = CalculateTotals(cart, discountAmount);

            // Validate payment
            if (model.AmountPaid < totals.TotalAmount)
            {
                return Json(new
                {
                    success = false,
                    message = $"Insufficient payment amount. Total: R {totals.TotalAmount:F2}, Paid: R {model.AmountPaid:F2}"
                });
            }

            // Save cart items for audit
            var cartSnapshot = cart.Select(c => new
            {
                c.ProductId,
                c.ItemName,
                c.Quantity,
                c.UnitPrice,
                c.Subtotal
            }).ToList();

            // Create sale
            var sale = new Sale
            {
                OrganizationId = organizationId,
                InvoiceNumber = GenerateInvoiceNumber(),
                CustomerId = model.CustomerId,
                SaleDate = DateTime.UtcNow,
                TotalAmount = totals.Subtotal,
                TaxAmount = totals.TaxAmount,
                DiscountAmount = discountAmount,
                NetAmount = totals.TotalAmount,
                PaymentMethod = Enum.Parse<PaymentMethod>(model.PaymentMethod),
                PaymentStatus = "Paid",
                UserId = userId,
                StoreId = storeId,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Sales.AddAsync(sale);
            await _unitOfWork.SaveChangesAsync();

            // Create sale items and update stock
            var soldItems = new List<string>();
            foreach (var item in cart)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);

                var saleItem = new SaleItem
                {
                    SaleId = sale.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Discount = item.Discount,
                    TaxAmount = item.TaxAmount,
                    Subtotal = item.Subtotal,
                    CostPriceAtSale = product?.CostPrice ?? 0,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.SaleItems.AddAsync(saleItem);

                // Update product stock
                if (product != null)
                {
                    var oldStock = product.CurrentStock;
                    product.CurrentStock -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Products.Update(product);
                    soldItems.Add($"{item.Quantity} x {product.ItemName}");

                    // Create stock movement
                    var stockMovement = new StockMovement
                    {
                        ProductId = item.ProductId,
                        MovementDate = DateTime.UtcNow,
                        MovementType = MovementType.Sale,
                        Quantity = -item.Quantity,
                        UnitPrice = item.UnitPrice,
                        StoreId = storeId,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        Reference = sale.InvoiceNumber,
                        Notes = $"Sale #{sale.InvoiceNumber}"
                    };

                    await _unitOfWork.StockMovements.AddAsync(stockMovement);
                }
            }

            // Update customer loyalty points
            if (model.CustomerId.HasValue)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(model.CustomerId.Value);
                if (customer != null)
                {
                    // Earn points (1 point per R10 spent)
                    var pointsEarned = (int)(totals.TotalAmount / 10);
                    var oldPoints = customer.LoyaltyPoints;
                    customer.LoyaltyPoints += pointsEarned;
                    customer.TotalSpent += totals.TotalAmount;
                    customer.LastPurchaseDate = DateTime.UtcNow;
                    customer.PointsTier = GetTier(customer.TotalSpent);

                    _unitOfWork.Customers.Update(customer);

                    // Create loyalty transaction
                    var loyaltyTransaction = new LoyaltyTransaction
                    {
                        CustomerId = customer.Id,
                        SaleId = sale.Id,
                        PointsEarned = pointsEarned,
                        PointsRedeemed = 0,
                        Balance = customer.LoyaltyPoints,
                        TransactionType = TransactionType.Earn,
                        Description = $"Purchase #{sale.InvoiceNumber}",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.LoyaltyTransactions.AddAsync(loyaltyTransaction);

                    // Log loyalty points
                    await LogAuditAsync(
                        "Earn",
                        "Loyalty",
                        customer.Id,
                        new { OldPoints = oldPoints },
                        new { NewPoints = customer.LoyaltyPoints, PointsEarned = pointsEarned },
                        $"Customer earned {pointsEarned} loyalty points. Total: {customer.LoyaltyPoints}",
                        sale.InvoiceNumber);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Clear cart
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("DiscountAmount");

            // Calculate change
            var change = model.AmountPaid - totals.TotalAmount;

            // Log sale
            await LogAuditAsync(
                "Create",
                "Sale",
                sale.Id,
                null,
                sale,
                $"Sale completed: {sale.InvoiceNumber}. Items: {string.Join(", ", soldItems)}. Total: R {sale.NetAmount:F2}. Payment: {sale.PaymentMethod}",
                sale.InvoiceNumber);

            return Json(new
            {
                success = true,
                invoiceNumber = sale.InvoiceNumber,
                totalAmount = totals.TotalAmount,
                change = change,
                message = "Sale completed successfully!"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in ProcessSale: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    // GET: POS/GetCart
    [HttpGet]
    public IActionResult GetCart()
    {
        var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
        var discountAmount = HttpContext.Session.GetDecimal("DiscountAmount") ?? 0;
        var totals = CalculateTotals(cart, discountAmount);

        return Json(new
        {
            cart = cart,
            subtotal = totals.Subtotal,
            taxAmount = totals.TaxAmount,
            discountAmount = discountAmount,
            totalAmount = totals.TotalAmount,
            itemCount = cart.Sum(c => c.Quantity)
        });
    }

    private string GenerateInvoiceNumber()
    {
        var date = DateTime.Now.ToString("yyyyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"INV-{date}-{random}";
    }

    private string GetTier(decimal totalSpent)
    {
        if (totalSpent >= 10000) return "Platinum";
        if (totalSpent >= 5000) return "Gold";
        if (totalSpent >= 1000) return "Silver";
        return "Bronze";
    }
}

// Session Extensions
public static class SessionExtensions
{
    public static void SetObjectAsJson(this ISession session, string key, object value)
    {
        session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value));
    }

    public static T? GetObjectFromJson<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : System.Text.Json.JsonSerializer.Deserialize<T>(value);
    }

    public static void SetDecimal(this ISession session, string key, decimal value)
    {
        session.SetString(key, value.ToString());
    }

    public static decimal? GetDecimal(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? null : decimal.TryParse(value, out var result) ? result : null;
    }
}