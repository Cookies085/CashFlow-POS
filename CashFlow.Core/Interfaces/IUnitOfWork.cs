using CashFlow.Core.Entities;

namespace CashFlow.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositories
    IRepository<Organization> Organizations { get; }
    IRepository<Store> Stores { get; }
    IRepository<User> Users { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<UserPermission> UserPermissions { get; }
    IRepository<Product> Products { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<Customer> Customers { get; }
    IRepository<Sale> Sales { get; }
    IRepository<SaleItem> SaleItems { get; }
    IRepository<StockMovement> StockMovements { get; }
    IRepository<LoyaltyTransaction> LoyaltyTransactions { get; }
    IRepository<GiftCard> GiftCards { get; }
    IRepository<Discount> Discounts { get; }
    IRepository<CustomerPayment> CustomerPayments { get; }
    IRepository<Receipt> Receipts { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<Shift> Shifts { get; }
    IRepository<Setting> Settings { get; }
    IRepository<StockCount> StockCounts { get; }
    IRepository<StockCountItem> StockCountItems { get; }

    Task<IEnumerable<StockMovement>> GetStockMovementsWithProductAsync(int organizationId);

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}