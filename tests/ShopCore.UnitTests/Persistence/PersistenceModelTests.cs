using Microsoft.EntityFrameworkCore;
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
        var pendingIndex = db.Model.FindEntityType(typeof(ShopCore.Domain.Entities.Order))!
            .GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(ShopCore.Domain.Entities.Order.CartVersion)));

        Assert.Equal("decimal(18,0)", price.GetColumnType());
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.True(rowVersion.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
        Assert.True(pendingIndex.IsUnique);
        Assert.Equal("[Status] = 'PendingPayment'", pendingIndex.GetFilter());
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
