using System.Security;
using CashFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    public DbSet<GiftCard> GiftCards { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<CustomerPayment> CustomerPayments { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<StockCount> StockCounts { get; set; }
    public DbSet<StockCountItem> StockCountItems { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<CashAccount> CashAccounts { get; set; }
    public DbSet<CashTransaction> CashTransactions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique constraints
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Name)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.OrganizationId, u.Username })
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.OrganizationId, p.ItemCode })
            .IsUnique();

        modelBuilder.Entity<Store>()
            .HasIndex(s => new { s.OrganizationId, s.Name })
            .IsUnique();

        modelBuilder.Entity<Supplier>()
            .HasIndex(s => new { s.OrganizationId, s.Name })
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.OrganizationId, c.Phone })
            .IsUnique();

        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<GiftCard>()
            .HasIndex(g => g.CardNumber)
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique();

        // User - Organization relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // User - Store relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.Store)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Store - Organization relationship
        modelBuilder.Entity<Store>()
            .HasOne(s => s.Organization)
            .WithMany(o => o.Stores)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Product - Organization relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Organization)
            .WithMany(o => o.Products)
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Product - Store relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Store)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Product - Supplier relationship
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        // Sale - Organization relationship
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Organization)
            .WithMany(o => o.Sales)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale - Customer relationship
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Sale - User relationship
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.User)
            .WithMany(u => u.Sales)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale - Store relationship
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Store)
            .WithMany(st => st.Sales)
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // SaleItem - Sale relationship
        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Sale)
            .WithMany(s => s.SaleItems)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // SaleItem - Product relationship
        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // StockMovement - Product relationship
        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // StockMovement - Store relationship
        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Store)
            .WithMany(s => s.StockMovements)
            .HasForeignKey(sm => sm.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Customer - Organization relationship
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.Organization)
            .WithMany(o => o.Customers)
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer - Store relationship
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.Store)
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // LoyaltyTransaction - Customer relationship
        modelBuilder.Entity<LoyaltyTransaction>()
            .HasOne(lt => lt.Customer)
            .WithMany(c => c.LoyaltyTransactions)
            .HasForeignKey(lt => lt.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // LoyaltyTransaction - Sale relationship
        modelBuilder.Entity<LoyaltyTransaction>()
            .HasOne(lt => lt.Sale)
            .WithMany(s => s.LoyaltyTransactions)
            .HasForeignKey(lt => lt.SaleId)
            .OnDelete(DeleteBehavior.SetNull);

        // GiftCard - Organization relationship
        modelBuilder.Entity<GiftCard>()
            .HasOne(g => g.Organization)
            .WithMany()
            .HasForeignKey(g => g.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // GiftCard - Customer relationship
        modelBuilder.Entity<GiftCard>()
            .HasOne(g => g.Customer)
            .WithMany(c => c.GiftCards)
            .HasForeignKey(g => g.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // GiftCard - Store relationship
        modelBuilder.Entity<GiftCard>()
            .HasOne(g => g.Store)
            .WithMany()
            .HasForeignKey(g => g.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Discount - Organization relationship
        modelBuilder.Entity<Discount>()
            .HasOne(d => d.Organization)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Discount - Product relationship
        modelBuilder.Entity<Discount>()
            .HasOne(d => d.Product)
            .WithMany(p => p.Discounts)
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // CustomerPayment - Customer relationship
        modelBuilder.Entity<CustomerPayment>()
            .HasOne(cp => cp.Customer)
            .WithMany(c => c.CustomerPayments)
            .HasForeignKey(cp => cp.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // CustomerPayment - Sale relationship
        modelBuilder.Entity<CustomerPayment>()
            .HasOne(cp => cp.Sale)
            .WithMany(s => s.CustomerPayments)
            .HasForeignKey(cp => cp.SaleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Receipt - Sale relationship
        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.Sale)
            .WithMany(s => s.Receipts)
            .HasForeignKey(r => r.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receipt - User relationship
        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.PrintedByUser)
            .WithMany()
            .HasForeignKey(r => r.PrintedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // UserPermission composite primary key
        modelBuilder.Entity<UserPermission>()
            .HasKey(up => new { up.UserId, up.PermissionId });

        // UserPermission - User relationship
        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserPermission - Permission relationship
        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        // AuditLog relationships
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.Store)
            .WithMany()
            .HasForeignKey(al => al.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.Organization)
            .WithMany(o => o.AuditLogs)
            .HasForeignKey(al => al.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Shift - Organization relationship
        modelBuilder.Entity<Shift>()
            .HasOne(s => s.Organization)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Shift - User relationship
        modelBuilder.Entity<Shift>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Shift - Store relationship
        modelBuilder.Entity<Shift>()
            .HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // Setting - Organization relationship
        modelBuilder.Entity<Setting>()
            .HasOne(s => s.Organization)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Setting - Store relationship
        modelBuilder.Entity<Setting>()
            .HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // StockCount relationships - Using NO ACTION to avoid cascade path issues
        modelBuilder.Entity<StockCount>()
            .HasOne(s => s.Organization)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockCount>()
            .HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<StockCount>()
            .HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);  

        modelBuilder.Entity<StockCount>()
            .HasOne(s => s.VerifiedByUser)
            .WithMany()
            .HasForeignKey(s => s.VerifiedBy)
            .OnDelete(DeleteBehavior.Restrict);   

        // StockCountItem relationships
        modelBuilder.Entity<StockCountItem>()
            .HasOne(sci => sci.StockCount)
            .WithMany(sc => sc.StockCountItems)
            .HasForeignKey(sci => sci.StockCountId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockCountItem>()
            .HasOne(sci => sci.Product)
            .WithMany()
            .HasForeignKey(sci => sci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // CashAccount - Organization
        modelBuilder.Entity<CashAccount>()
            .HasOne(ca => ca.Organization)
            .WithMany()
            .HasForeignKey(ca => ca.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashAccount>()
            .HasOne(ca => ca.Store)
            .WithMany()
            .HasForeignKey(ca => ca.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        // CashTransaction - CashAccount
        modelBuilder.Entity<CashTransaction>()
            .HasOne(ct => ct.CashAccount)
            .WithMany()
            .HasForeignKey(ct => ct.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}