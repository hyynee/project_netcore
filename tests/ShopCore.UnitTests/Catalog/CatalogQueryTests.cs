using Microsoft.EntityFrameworkCore;
using ShopCore.Api.Exceptions;
using ShopCore.Api.Services;
using ShopCore.Application.Contracts;
using ShopCore.Infrastructure.Persistence;

namespace ShopCore.UnitTests.Catalog;

public sealed class CatalogQueryTests
{
    private const string UnreachableConnectionString =
        "Server=localhost,1433;Database=ShopCore_ValidationOnly;User Id=sa;Password=Your_strong_password123!;TrustServerCertificate=True";

    [Fact]
    public async Task GetProductsAsync_WhenPageIsBelowOne_ThrowsValidationException()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetProductsAsync(new ProductQueryRequest(Page: 0), CancellationToken.None));

        Assert.Contains("Page", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetProductsAsync_WhenPageSizeExceedsMaximum_ThrowsValidationException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetProductsAsync(new ProductQueryRequest(PageSize: 101), CancellationToken.None));
    }

    [Fact]
    public async Task GetProductsAsync_WhenSortIsNotAllowListed_ThrowsValidationException()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetProductsAsync(new ProductQueryRequest(Sort: "stock"), CancellationToken.None));

        Assert.Contains("sort", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CatalogQuery_IsFilteredAndPaginatedInsideTheDatabase()
    {
        using var db = CreateContext();
        var query = db.Products.AsNoTracking()
            .Where(product => product.IsActive && product.Category.IsActive)
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip(20)
            .Take(20);

        var sql = query.ToQueryString();

        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN [Categories]", sql, StringComparison.Ordinal);
        Assert.Contains("[p].[IsActive] = CAST(1 AS bit)", sql, StringComparison.Ordinal);
        Assert.Contains("[c].[IsActive] = CAST(1 AS bit)", sql, StringComparison.Ordinal);
        Assert.Contains("OFFSET", sql, StringComparison.Ordinal);
        Assert.Contains("FETCH NEXT", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogSearch_UsesTheSearchTermIndexInsteadOfScanningProductNames()
    {
        using var db = CreateContext();
        var tokens = CatalogQueryGuard.ResolveSearchTokens("shirt");
        var query = CatalogSearchFilter.Apply(
            db.Products.AsNoTracking().Where(product => product.IsActive),
            db.ProductSearchTerms.AsNoTracking(),
            tokens!);

        var sql = query.ToQueryString();

        Assert.Contains("[ProductSearchTerms]", sql, StringComparison.Ordinal);
        Assert.Contains("LIKE @__lastToken_0_startswith", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("[p].[Name] LIKE", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogSearch_ParameterizesTheUserTerm()
    {
        using var db = CreateContext();
        var tokens = CatalogQueryGuard.ResolveSearchTokens("shirt' OR 1=1 --");
        var query = CatalogSearchFilter.Apply(
            db.Products.AsNoTracking().Where(product => product.IsActive),
            db.ProductSearchTerms.AsNoTracking(),
            tokens!);

        var sql = query.ToQueryString();

        Assert.DoesNotContain("OR 1=1", sql, StringComparison.Ordinal);
        Assert.Contains("@", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogSearch_WithoutIndexableTokenMatchesNothing()
    {
        using var db = CreateContext();
        var tokens = CatalogQueryGuard.ResolveSearchTokens("%%%");
        var query = CatalogSearchFilter.Apply(
            db.Products.AsNoTracking().Where(product => product.IsActive),
            db.ProductSearchTerms.AsNoTracking(),
            tokens!);

        var sql = query.ToQueryString();

        Assert.Contains("WHERE 0 = 1", sql, StringComparison.Ordinal);
    }

    private static CatalogService CreateService() => new(CreateContext());

    private static ShopCoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopCoreDbContext>()
            .UseSqlServer(UnreachableConnectionString)
            .Options;
        return new ShopCoreDbContext(options, TimeProvider.System);
    }
}
