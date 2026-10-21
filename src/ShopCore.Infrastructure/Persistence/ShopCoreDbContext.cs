using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ShopCore.Domain.Entities;
using ShopCore.Domain.Enums;

namespace ShopCore.Infrastructure.Persistence;

public sealed class ShopCoreDbContext(DbContextOptions<ShopCoreDbContext> options, TimeProvider timeProvider)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<ReconciliationIssue> ReconciliationIssues => Set<ReconciliationIssue>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserShippingAddress> UserShippingAddresses => Set<UserShippingAddress>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureUsers(builder);
        ConfigureUserProfile(builder);
        ConfigureCatalog(builder);
        ConfigureCart(builder);
        ConfigureOrders(builder);
        ConfigurePayments(builder);
        ConfigureInventory(builder);
        ConfigureSupportTables(builder);
    }

    public override int SaveChanges()
    {
        ApplyAuditValues();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditValues();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditValues()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedBy).IsModified = false;
            }
        }
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<ApplicationUser>();
        user.ToTable("ApplicationUsers");
        user.Property(x => x.Email).HasMaxLength(320).IsRequired();
        user.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        user.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        user.Property(x => x.NormalizedUserName).HasMaxLength(100).IsRequired();
        user.Property(x => x.FirstName).HasMaxLength(100);
        user.Property(x => x.LastName).HasMaxLength(100);
        user.HasIndex(x => x.NormalizedEmail).IsUnique();
        user.HasOne(x => x.Cart).WithOne(x => x.User).HasForeignKey<Cart>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        user.HasMany(x => x.Orders).WithOne(x => x.User)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        user.HasMany(x => x.RefreshTokens).WithOne(x => x.User)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        user.HasMany(x => x.IdempotencyRecords).WithOne(x => x.User)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        user.HasOne(x => x.ShippingAddress).WithOne(x => x.User)
            .HasForeignKey<UserShippingAddress>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserProfile(ModelBuilder modelBuilder)
    {
        var addr = modelBuilder.Entity<UserShippingAddress>();
        addr.ToTable("UserShippingAddresses");
        addr.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        addr.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        addr.Property(x => x.AddressLine1).HasMaxLength(500).IsRequired();
        addr.Property(x => x.Ward).HasMaxLength(150).IsRequired();
        addr.Property(x => x.District).HasMaxLength(150).IsRequired();
        addr.Property(x => x.Province).HasMaxLength(150).IsRequired();
        addr.Property(x => x.PostalCode).HasMaxLength(20);
        addr.HasIndex(x => x.UserId).IsUnique();
    }

    private static void ConfigureCatalog(ModelBuilder modelBuilder)
    {
        var category = modelBuilder.Entity<Category>();
        category.Property(x => x.Name).HasMaxLength(200).IsRequired();
        category.HasIndex(x => x.Name).IsUnique();

        var product = modelBuilder.Entity<Product>();
        product.Property(x => x.Name).HasMaxLength(250).IsRequired();
        product.Property(x => x.Description).HasMaxLength(4000);
        product.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        product.Property(x => x.Price).HasPrecision(18, 0);
        product.Property(x => x.RowVersion).IsRowVersion();
        product.HasIndex(x => new { x.CategoryId, x.IsActive });
        product.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[Stock] >= 0");
            table.HasCheckConstraint("CK_Products_Reserved_NonNegative", "[ReservedQuantity] >= 0");
            table.HasCheckConstraint("CK_Products_Reserved_NotGreaterThanStock",
                "[ReservedQuantity] <= [Stock]");
        });
        category.HasMany(x => x.Products).WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureCart(ModelBuilder modelBuilder)
    {
        var cart = modelBuilder.Entity<Cart>();
        cart.HasIndex(x => x.UserId).IsUnique();
        cart.Property(x => x.RowVersion).IsRowVersion();
        cart.ToTable(table => table.HasCheckConstraint("CK_Carts_Version_NonNegative", "[Version] >= 0"));

        var item = modelBuilder.Entity<CartItem>();
        item.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        item.ToTable(table => table.HasCheckConstraint("CK_CartItems_Quantity_Range", "[Quantity] BETWEEN 1 AND 99"));
        item.HasOne(x => x.Product).WithMany(x => x.CartItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        order.Property(x => x.TotalAmount).HasPrecision(18, 0);
        order.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        order.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        order.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        order.Property(x => x.AddressLine1).HasMaxLength(500).IsRequired();
        order.Property(x => x.Ward).HasMaxLength(150).IsRequired();
        order.Property(x => x.District).HasMaxLength(150).IsRequired();
        order.Property(x => x.Province).HasMaxLength(150).IsRequired();
        order.Property(x => x.PostalCode).HasMaxLength(20);
        order.HasIndex(x => new { x.UserId, x.CreatedAt });
        order.HasIndex(x => new { x.UserId, x.CartVersion })
            .HasFilter("[Status] = 'PendingPayment'")
            .IsUnique();
        order.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
            table.HasCheckConstraint("CK_Orders_CartVersion_NonNegative", "[CartVersion] >= 0");
        });
        order.HasOne(x => x.Payment).WithOne(x => x.Order)
            .HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        order.HasMany(x => x.Items).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        order.HasMany(x => x.InventoryReservations).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        order.HasMany(x => x.InventoryAdjustments).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        order.HasMany(x => x.StatusHistory).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        order.HasMany(x => x.ReconciliationIssues).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);

        var item = modelBuilder.Entity<OrderItem>();
        item.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        item.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsRequired();
        item.Property(x => x.UnitPrice).HasPrecision(18, 0);
        item.Property(x => x.LineTotal).HasPrecision(18, 0);
        item.ToTable(table =>
        {
            table.HasCheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_OrderItems_LineTotal_NonNegative", "[LineTotal] >= 0");
            table.HasCheckConstraint("CK_OrderItems_Quantity_Range", "[Quantity] BETWEEN 1 AND 99");
        });
        item.HasOne(x => x.Product).WithMany(x => x.OrderItems)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        var payment = modelBuilder.Entity<Payment>();
        payment.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        payment.Property(x => x.StripeCheckoutSessionId).HasMaxLength(255);
        payment.Property(x => x.StripePaymentIntentId).HasMaxLength(255);
        payment.HasIndex(x => x.StripeCheckoutSessionId).IsUnique()
            .HasFilter("[StripeCheckoutSessionId] IS NOT NULL");
        payment.HasIndex(x => x.StripePaymentIntentId).IsUnique()
            .HasFilter("[StripePaymentIntentId] IS NOT NULL");

        var webhook = modelBuilder.Entity<PaymentWebhookEvent>();
        webhook.Property(x => x.StripeEventId).HasMaxLength(255).IsRequired();
        webhook.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        webhook.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        webhook.Property(x => x.LastError).HasMaxLength(2000);
        webhook.HasIndex(x => x.StripeEventId).IsUnique();
        payment.HasMany(x => x.WebhookEvents).WithOne(x => x.Payment)
            .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        var reservation = modelBuilder.Entity<InventoryReservation>();
        reservation.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        reservation.Property(x => x.OperationReference).HasMaxLength(100).IsRequired();
        reservation.HasIndex(x => x.OperationReference).IsUnique();
        reservation.HasIndex(x => new { x.OrderId, x.ProductId });
        reservation.ToTable(table =>
            table.HasCheckConstraint("CK_InventoryReservations_Quantity_Positive", "[Quantity] > 0"));
        reservation.HasOne(x => x.Product).WithMany(x => x.InventoryReservations)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        var adjustment = modelBuilder.Entity<InventoryAdjustment>();
        adjustment.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        adjustment.Property(x => x.OperationReference).HasMaxLength(100).IsRequired();
        adjustment.HasIndex(x => x.OperationReference).IsUnique();
        adjustment.HasOne(x => x.Product).WithMany(x => x.InventoryAdjustments)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        adjustment.HasOne(x => x.Order).WithMany(x => x.InventoryAdjustments)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        adjustment.ToTable(table =>
            table.HasCheckConstraint("CK_InventoryAdjustments_QuantityDelta_NonZero", "[QuantityDelta] <> 0"));
    }

    private static void ConfigureSupportTables(ModelBuilder modelBuilder)
    {
        var history = modelBuilder.Entity<OrderStatusHistory>();
        history.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(32);
        history.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        history.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        history.Property(x => x.ActorId).HasMaxLength(100);
        history.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();

        var idempotency = modelBuilder.Entity<IdempotencyRecord>();
        idempotency.Property(x => x.Key).HasMaxLength(255).IsRequired();
        idempotency.Property(x => x.RequestFingerprint).HasMaxLength(128).IsRequired();
        idempotency.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        idempotency.Property(x => x.ResponsePayload).HasMaxLength(4000);
        idempotency.HasIndex(x => new { x.UserId, x.Key }).IsUnique();

        var issue = modelBuilder.Entity<ReconciliationIssue>();
        issue.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        issue.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        issue.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        issue.Property(x => x.StripeObjectId).HasMaxLength(255);
        issue.Property(x => x.LastError).HasMaxLength(2000);
        issue.Property(x => x.ResolutionNote).HasMaxLength(2000);
        issue.Property(x => x.ResolvedBy).HasMaxLength(100);

        var refreshToken = modelBuilder.Entity<RefreshToken>();
        refreshToken.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        refreshToken.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
        refreshToken.Property(x => x.RowVersion).IsRowVersion();
        refreshToken.HasIndex(x => x.TokenHash).IsUnique();
        refreshToken.HasIndex(x => x.FamilyId);
    }
}
