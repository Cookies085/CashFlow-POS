using CashFlow.Core.Entities;
using CashFlow.Core.Interfaces;
using CashFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CashFlow.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    // Repositories
    private IRepository<Organization>? _organizations;
    private IRepository<Store>? _stores;
    private IRepository<User>? _users;
    private IRepository<Permission>? _permissions;
    private IRepository<UserPermission>? _userPermissions;
    private IRepository<Product>? _products;
    private IRepository<Supplier>? _suppliers;
    private IRepository<Customer>? _customers;
    private IRepository<Sale>? _sales;
    private IRepository<SaleItem>? _saleItems;
    private IRepository<StockMovement>? _stockMovements;
    private IRepository<LoyaltyTransaction>? _loyaltyTransactions;
    private IRepository<GiftCard>? _giftCards;
    private IRepository<Discount>? _discounts;
    private IRepository<CustomerPayment>? _customerPayments;
    private IRepository<Receipt>? _receipts;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<Shift>? _shifts;
    private IRepository<Setting>? _settings;
    private IRepository<StockCount>? _stockCounts;
    private IRepository<StockCountItem>? _stockCountItems;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<Organization> Organizations =>
        _organizations ??= new Repository<Organization>(_context);

    public IRepository<Store> Stores =>
        _stores ??= new Repository<Store>(_context);

    public IRepository<User> Users =>
        _users ??= new Repository<User>(_context);

    public IRepository<Permission> Permissions =>
        _permissions ??= new Repository<Permission>(_context);

    public IRepository<UserPermission> UserPermissions =>
        _userPermissions ??= new Repository<UserPermission>(_context);

    public IRepository<Product> Products =>
        _products ??= new Repository<Product>(_context);

    public IRepository<Supplier> Suppliers =>
        _suppliers ??= new Repository<Supplier>(_context);

    public IRepository<Customer> Customers =>
        _customers ??= new Repository<Customer>(_context);

    public IRepository<Sale> Sales =>
        _sales ??= new Repository<Sale>(_context);

    public IRepository<SaleItem> SaleItems =>
        _saleItems ??= new Repository<SaleItem>(_context);

    public IRepository<StockMovement> StockMovements =>
        _stockMovements ??= new Repository<StockMovement>(_context);

    public IRepository<LoyaltyTransaction> LoyaltyTransactions =>
        _loyaltyTransactions ??= new Repository<LoyaltyTransaction>(_context);

    public IRepository<GiftCard> GiftCards =>
        _giftCards ??= new Repository<GiftCard>(_context);

    public IRepository<Discount> Discounts =>
        _discounts ??= new Repository<Discount>(_context);

    public IRepository<CustomerPayment> CustomerPayments =>
        _customerPayments ??= new Repository<CustomerPayment>(_context);

    public IRepository<Receipt> Receipts =>
        _receipts ??= new Repository<Receipt>(_context);

    public IRepository<AuditLog> AuditLogs =>
        _auditLogs ??= new Repository<AuditLog>(_context);

    public IRepository<Shift> Shifts =>   
        _shifts ??= new Repository<Shift>(_context);

    public IRepository<Setting> Settings =>
        _settings ??= new Repository<Setting>(_context);

    public IRepository<StockCount> StockCounts =>
        _stockCounts ??= new Repository<StockCount>(_context);

    public IRepository<StockCountItem> StockCountItems =>
        _stockCountItems ??= new Repository<StockCountItem>(_context);

    public async Task<IEnumerable<StockMovement>> GetStockMovementsWithProductAsync(int organizationId)
    {
        return await _context.StockMovements
            .Include(sm => sm.Product)
            .Where(sm => sm.Product.OrganizationId == organizationId)
            .OrderByDescending(sm => sm.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}