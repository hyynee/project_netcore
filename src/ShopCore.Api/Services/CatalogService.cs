using Microsoft.EntityFrameworkCore;
using ShopCore.Api.Exceptions;
using ShopCore.Application.Catalog;
using ShopCore.Application.Contracts;
using ShopCore.Domain.Entities;
using ShopCore.Infrastructure.Persistence;

namespace ShopCore.Api.Services;

public sealed class CatalogService(ShopCoreDbContext db) : ICatalogService
{
    public async Task<CatalogPage<ProductSummary>> GetProductsAsync(
        ProductQueryRequest query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = CatalogQueryGuard.ResolvePaging(query.Page, query.PageSize);
        var sortOption = CatalogQueryGuard.ParseSort(query.Sort);
        var searchTokens = CatalogQueryGuard.ResolveSearchTokens(query.Search);

        var products = db.Products.AsNoTracking()
            .Where(product => product.IsActive && product.Category.IsActive);

        if (query.CategoryId is Guid categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (searchTokens is not null)
        {
            products = CatalogSearchFilter.Apply(
                products,
                db.ProductSearchTerms.AsNoTracking(),
                searchTokens);
        }

        products = ApplySort(products, sortOption);

        var totalCount = await products.CountAsync(cancellationToken);
        var items = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductSummary(
                product.Id,
                product.CategoryId,
                product.Category.Name,
                product.Name,
                product.Description,
                product.Price,
                product.Currency,
                product.Stock - product.ReservedQuantity))
            .ToListAsync(cancellationToken);

        return new CatalogPage<ProductSummary>(
            items,
            page,
            pageSize,
            totalCount,
            CatalogQueryGuard.TotalPages(totalCount, pageSize));
    }

    public async Task<ProductDetails> GetPublicProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Where(product => product.Id == id && product.IsActive && product.Category.IsActive)
            .Select(product => new
            {
                product.Id,
                product.CategoryId,
                CategoryName = product.Category.Name,
                product.Name,
                product.Description,
                product.Price,
                product.Currency,
                AvailableQuantity = product.Stock - product.ReservedQuantity
            })
            .SingleOrDefaultAsync(cancellationToken);

        return product is null
            ? throw new NotFoundException("Product not found.")
            : new ProductDetails(
                product.Id,
                product.CategoryId,
                product.CategoryName,
                product.Name,
                product.Description,
                product.Price,
                product.Currency,
                product.AvailableQuantity);
    }

    public async Task<CatalogPage<CategoryResponse>> GetActiveCategoriesAsync(
        CategoryQueryRequest query,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = CatalogQueryGuard.ResolvePaging(query.Page, query.PageSize);

        var categories = db.Categories.AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id);

        var totalCount = await categories.CountAsync(cancellationToken);
        var items = await categories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.IsActive))
            .ToListAsync(cancellationToken);

        return new CatalogPage<CategoryResponse>(
            items,
            page,
            pageSize,
            totalCount,
            CatalogQueryGuard.TotalPages(totalCount, pageSize));
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> query, CatalogSortOption option) => option switch
    {
        CatalogSortOption.Name =>
            query.OrderBy(product => product.Name).ThenBy(product => product.Id),
        CatalogSortOption.PriceAscending =>
            query.OrderBy(product => product.Price).ThenBy(product => product.Id),
        CatalogSortOption.PriceDescending =>
            query.OrderByDescending(product => product.Price).ThenBy(product => product.Id),
        _ => query.OrderByDescending(product => product.CreatedAt).ThenBy(product => product.Id)
    };
}
