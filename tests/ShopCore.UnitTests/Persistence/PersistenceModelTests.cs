using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ShopCore.Infrastructure.Persistence;

namespace ShopCore.UnitTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void ModelUsesRequiredVndPrecisionAndConcurrencyTokens()
    {
        var options = new DbContextOptionsBuilder<ShopCoreDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ShopCore")
            .Options;
        using var db = new ShopCoreDbContext(options, TimeProvider.System);

        var product = db.Model.FindEntityType(typeof(ShopCore.Domain.Entities.Product))!;
        var price = product.FindProperty(nameof(ShopCore.Domain.Entities.Product.Price))!;
        var rowVersion = product.FindProperty(nameof(ShopCore.Domain.Entities.Product.RowVersion))!;
        var refreshToken = db.Model.FindEntityType(typeof(ShopCore.Domain.Entities.RefreshToken))!;
        var refreshTokenRowVersion = refreshToken.FindProperty(nameof(ShopCore.Domain.Entities.RefreshToken.RowVersion))!;
        var pendingIndex = db.Model.FindEntityType(typeof(ShopCore.Domain.Entities.Order))!
            .GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(ShopCore.Domain.Entities.Order.CartVersion)));

        Assert.Equal("decimal(18,0)", price.GetColumnType());
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.True(rowVersion.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
        Assert.True(refreshTokenRowVersion.IsConcurrencyToken);
        Assert.True(refreshTokenRowVersion.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
        Assert.True(pendingIndex.IsUnique);
        Assert.Equal("[Status] = 'PendingPayment'", pendingIndex.GetFilter());
    }

    [Fact]
    public void CatalogModelUsesIndexesAndConstraintsForPublicQueries()
    {
        var options = new DbContextOptionsBuilder<ShopCoreDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ShopCore")
            .Options;
        using var db = new ShopCoreDbContext(options, TimeProvider.System);
        var model = db.GetService<IDesignTimeModel>().Model;

        var product = model.FindEntityType(typeof(ShopCore.Domain.Entities.Product))!;
        var category = model.FindEntityType(typeof(ShopCore.Domain.Entities.Category))!;
        var adjustment = model.FindEntityType(typeof(ShopCore.Domain.Entities.InventoryAdjustment))!;

        var productIndexes = product.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToList();
        var categoryIndexes = category.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToList();
        var productConstraints = product.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToList();
        var adjustmentType = adjustment.FindProperty(nameof(ShopCore.Domain.Entities.InventoryAdjustment.Type))!;

        Assert.Contains("CategoryId,IsActive", productIndexes);
        Assert.Contains("IsActive,Name", productIndexes);
        Assert.Contains("IsActive,CategoryId,Name", productIndexes);
        Assert.Contains("IsActive,Price", productIndexes);
        Assert.Contains("IsActive,CreatedAt", productIndexes);
        Assert.Contains("IsActive,Name", categoryIndexes);

        Assert.Contains("CK_Products_Price_Positive", productConstraints);
        Assert.Contains("CK_Products_Stock_NonNegative", productConstraints);
        Assert.Contains("CK_Products_Reserved_NonNegative", productConstraints);
        Assert.Contains("CK_Products_Reserved_NotGreaterThanStock", productConstraints);

        Assert.Equal("nvarchar(32)", adjustmentType.GetColumnType());
        Assert.Empty(product.GetSeedData());
        Assert.Empty(category.GetSeedData());
    }

    [Fact]
    public void CatalogModelUsesTheProductSearchTermIndex()
    {
        var options = new DbContextOptionsBuilder<ShopCoreDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ShopCore")
            .Options;
        using var db = new ShopCoreDbContext(options, TimeProvider.System);
        var model = db.GetService<IDesignTimeModel>().Model;

        var searchTerm = model.FindEntityType(typeof(ShopCore.Domain.Entities.ProductSearchTerm))!;
        var searchIndexes = searchTerm.GetIndexes()
            .Select(index => string.Join(",", index.Properties.Select(property => property.Name)))
            .ToList();
        var keyProperties = searchTerm.FindPrimaryKey()!.Properties
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(["ProductId", "Term"], keyProperties);
        Assert.Contains("Term,ProductId", searchIndexes);
    }

    [SqlServerFact]
    public async Task SqlServerFixtureMigratesAndReadsSeededProduct()
    {
        await using var fixture = new SqlServerFixture();
        try
        {
            await fixture.InitializeAsync();
        }
        catch (DotNet.Testcontainers.Builders.DockerUnavailableException)
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "Docker is unavailable; start Docker and rerun with SHOPCORE_RUN_SQL_TESTS=true.");
        }
        catch (AggregateException exception)
            when (exception.InnerExceptions.Any(
                inner => inner is DotNet.Testcontainers.Builders.DockerUnavailableException))
        {
            throw Xunit.Sdk.SkipException.ForSkip(
                "Docker is unavailable; start Docker and rerun with SHOPCORE_RUN_SQL_TESTS=true.");
        }

        await fixture.ResetAsync();
        var seeded = await fixture.SeedActiveProductAsync();

        await using var db = fixture.CreateContext();
        var product = await db.Products.AsNoTracking()
            .SingleAsync(product => product.Id == seeded.Id);

        Assert.Equal("VND", product.Currency);
        Assert.Equal(1, product.Stock);
    }
}
