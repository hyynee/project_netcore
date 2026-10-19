using Microsoft.EntityFrameworkCore;
using ShopCore.Domain.Entities;
using ShopCore.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace ShopCore.UnitTests.Persistence;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string Password = "Your_strong_password123!";
    private MsSqlContainer? _container;

    public static string? SkipReason =>
        string.Equals(Environment.GetEnvironmentVariable("SHOPCORE_RUN_SQL_TESTS"), "true",
            StringComparison.OrdinalIgnoreCase)
            ? null
            : "Set SHOPCORE_RUN_SQL_TESTS=true and ensure Docker is available.";

    public string ConnectionString =>
        (_container ?? throw new InvalidOperationException("SQL Server container is not initialized."))
        .GetConnectionString();

    public async Task InitializeAsync()
    {
        if (SkipReason is not null)
        {
            return;
        }

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(Password)
            .Build();
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (SkipReason is null)
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }

    public ShopCoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopCoreDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ShopCoreDbContext(options, TimeProvider.System);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM [OrderStatusHistory];
            DELETE FROM [ReconciliationIssues];
            DELETE FROM [InventoryAdjustments];
            DELETE FROM [InventoryReservations];
            DELETE FROM [PaymentWebhookEvents];
            DELETE FROM [OrderItems];
            DELETE FROM [Payments];
            DELETE FROM [Orders];
            DELETE FROM [CartItems];
            DELETE FROM [Carts];
            DELETE FROM [Products];
            DELETE FROM [Categories];
            DELETE FROM [RefreshTokens];
            DELETE FROM [IdempotencyRecords];
            DELETE FROM [ApplicationUsers];
            """,
            cancellationToken);
    }

    public async Task<Product> SeedActiveProductAsync(
        int stock = 1,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext();
        var category = new Category { Name = $"Test-{Guid.NewGuid():N}" };
        var product = new Product
        {
            Category = category,
            Name = "Test product",
            Price = 100_000,
            Currency = "VND",
            Stock = stock,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        return product;
    }
}
