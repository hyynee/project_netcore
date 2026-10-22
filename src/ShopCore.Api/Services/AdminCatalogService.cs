using Microsoft.EntityFrameworkCore;
using ShopCore.Api.Exceptions;
using ShopCore.Application.Catalog;
using ShopCore.Application.Common;
using ShopCore.Application.Contracts;
using ShopCore.Domain.Entities;
using ShopCore.Domain.Enums;
using ShopCore.Infrastructure.Persistence;

namespace ShopCore.Api.Services;

public sealed class AdminCatalogService(ShopCoreDbContext db, ICurrentUser currentUser) : IAdminCatalogService
{
    public async Task<CatalogPage<AdminProductSummary>> GetProductsAsync(
        AdminProductQueryRequest query,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var (page, pageSize) = CatalogQueryGuard.ResolvePaging(query.Page, query.PageSize);
        var sortOption = CatalogQueryGuard.ParseSort(query.Sort);
        var searchTokens = CatalogQueryGuard.ResolveSearchTokens(query.Search);

        var products = db.Products.AsNoTracking();

        if (!query.IncludeInactive)
        {
            products = products.Where(product => product.IsActive);
        }

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
        var rows = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new
            {
                product.Id,
                product.CategoryId,
                CategoryName = product.Category.Name,
                product.Name,
                product.Price,
                product.Currency,
                product.Stock,
                product.ReservedQuantity,
                product.IsActive,
                product.UpdatedAt,
                product.RowVersion
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new AdminProductSummary(
                row.Id,
                row.CategoryId,
                row.CategoryName,
                row.Name,
                row.Price,
                row.Currency,
                row.Stock,
                row.ReservedQuantity,
                row.Stock - row.ReservedQuantity,
                row.IsActive,
                row.UpdatedAt,
                Convert.ToBase64String(row.RowVersion)))
            .ToList();

        return new CatalogPage<AdminProductSummary>(
            items,
            page,
            pageSize,
            totalCount,
            CatalogQueryGuard.TotalPages(totalCount, pageSize));
    }

    public async Task<AdminProductDetails> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var product = await db.Products.AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new
            {
                product.Id,
                product.CategoryId,
                CategoryName = product.Category.Name,
                product.Name,
                product.Description,
                product.Price,
                product.Currency,
                product.Stock,
                product.ReservedQuantity,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt,
                product.RowVersion
            })
            .SingleOrDefaultAsync(cancellationToken);

        return product is null
            ? throw new NotFoundException("Product not found.")
            : new AdminProductDetails(
                product.Id,
                product.CategoryId,
                product.CategoryName,
                product.Name,
                product.Description,
                product.Price,
                product.Currency,
                product.Stock,
                product.ReservedQuantity,
                product.Stock - product.ReservedQuantity,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt,
                Convert.ToBase64String(product.RowVersion));
    }

    public async Task<AdminProductDetails> CreateProductAsync(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);
        EnsureValidPrice(request.Price);
        var product = new Product
        {
            CategoryId = category.Id,
            Name = NormalizeName(request.Name),
            Description = NormalizeDescription(request.Description),
            Price = request.Price,
            Currency = NormalizeCurrency(request.Currency),
            Stock = request.Stock
        };

        db.Products.Add(product);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The product was modified by another administrator.");
        }

        return ToDetails(product, category.Name);
    }

    public async Task<AdminProductDetails> UpdateProductAsync(
        Guid id,
        ProductUpdateRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Product not found.");
        var category = await GetActiveCategoryAsync(request.CategoryId, cancellationToken);
        ApplyExpectedRowVersion(product, request.RowVersion);
        EnsureValidPrice(request.Price);

        if (request.Stock < product.ReservedQuantity)
        {
            throw new ValidationException("Stock cannot be lower than the reserved quantity.");
        }

        product.CategoryId = category.Id;
        product.Name = NormalizeName(request.Name);
        product.Description = NormalizeDescription(request.Description);
        product.Price = request.Price;
        product.Currency = NormalizeCurrency(request.Currency);
        product.Stock = request.Stock;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The product was modified by another administrator. Reload the product and retry.");
        }

        return ToDetails(product, category.Name);
    }

    public async Task DeactivateProductAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        if (!product.IsActive)
        {
            return;
        }

        product.IsActive = false;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The product was modified by another administrator. Reload the product and retry.");
        }
    }

    public async Task<InventoryAdjustmentResult> AdjustInventoryAsync(
        Guid productId,
        InventoryAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        if (request.QuantityDelta == 0)
        {
            throw new ValidationException("QuantityDelta must not be zero.");
        }

        if (!Enum.IsDefined(request.Type))
        {
            throw new ValidationException($"Type must be one of: {string.Join(", ", Enum.GetNames<InventoryAdjustmentType>())}.");
        }

        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var newStock = (long)product.Stock + request.QuantityDelta;
        if (newStock < product.ReservedQuantity)
        {
            throw new ValidationException("The adjustment would set stock below the reserved quantity.");
        }

        if (newStock > int.MaxValue)
        {
            throw new ValidationException("The adjustment would exceed the maximum stock value.");
        }

        product.Stock = (int)newStock;

        var actorId = currentUser.UserId;
        var adjustment = new InventoryAdjustment
        {
            ProductId = productId,
            QuantityDelta = request.QuantityDelta,
            Type = request.Type,
            Reason = request.Reason.Trim(),
            OperationReference = request.OperationReference.Trim(),
            CreatedBy = actorId
        };
        db.InventoryAdjustments.Add(adjustment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "The product was modified by another administrator. Reload the product and retry.");
        }

        return new InventoryAdjustmentResult(
            adjustment.Id,
            productId,
            adjustment.QuantityDelta,
            adjustment.Type,
            product.Stock,
            product.ReservedQuantity,
            adjustment.CreatedAt,
            actorId);
    }

    public async Task<CatalogPage<CategoryResponse>> GetCategoriesAsync(
        CategoryQueryRequest query,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var (page, pageSize) = CatalogQueryGuard.ResolvePaging(query.Page, query.PageSize);

        var categories = db.Categories.AsNoTracking()
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

    public async Task<CategoryResponse> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        return await db.Categories.AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Category not found.");
    }

    public async Task<CategoryResponse> CreateCategoryAsync(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var category = new Category { Name = NormalizeName(request.Name) };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(category.Id, category.Name, category.IsActive);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(
        Guid id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");
        category.Name = NormalizeName(request.Name);
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(category.Id, category.Name, category.IsActive);
    }

    public async Task<CategoryDeletionResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        var hasProducts = await db.Products.AnyAsync(product => product.CategoryId == id, cancellationToken);
        if (hasProducts)
        {
            category.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            return new CategoryDeletionResult(category.Id, category.IsActive, Deactivated: true);
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return new CategoryDeletionResult(category.Id, IsActive: false, Deactivated: false);
    }

    public async Task<SearchReindexResult> ReindexProductSearchAsync(CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var products = await db.Products
            .AsNoTracking()
            .Select(product => new { product.Id, product.Name })
            .ToListAsync(cancellationToken);

        db.ChangeTracker.Clear();
        await db.ProductSearchTerms.ExecuteDeleteAsync(cancellationToken);

        var terms = products
            .SelectMany(product => ProductSearchTokenizer.Tokenize(product.Name)
                .Select(term => new ProductSearchTerm { ProductId = product.Id, Term = term }));
        db.ProductSearchTerms.AddRange(terms);
        await db.SaveChangesAsync(cancellationToken);

        return new SearchReindexResult(products.Count, terms.Count());
    }

    private void EnsureAdmin()
    {
        if (!currentUser.IsInRole(ShopRoles.Admin))
        {
            throw new ForbiddenException("The administrator role is required to manage the catalog.");
        }
    }

    private async Task<Category> GetActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        await db.Categories.SingleOrDefaultAsync(
            category => category.Id == categoryId && category.IsActive,
            cancellationToken) ?? throw new ValidationException("Category is invalid or inactive.");

    private static AdminProductDetails ToDetails(Product product, string categoryName) => new(
        product.Id,
        product.CategoryId,
        categoryName,
        product.Name,
        product.Description,
        product.Price,
        product.Currency,
        product.Stock,
        product.ReservedQuantity,
        product.Stock - product.ReservedQuantity,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt,
        Convert.ToBase64String(product.RowVersion));

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

    private void ApplyExpectedRowVersion(Product product, string rowVersion)
    {
        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ValidationException("RowVersion must be a valid base64 value.");
        }

        if (expected.Length != 8)
        {
            throw new ValidationException("RowVersion must be a valid base64 value.");
        }

        db.Entry(product).Property(entity => entity.RowVersion).OriginalValue = expected;
    }

    private static void EnsureValidPrice(decimal price)
    {
        if (!PositiveAmountAttribute.IsWholePositive(price))
        {
            throw new ValidationException("Price must be a whole number greater than zero.");
        }
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (normalized.Length == 0)
        {
            throw new ValidationException("Name is required.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (description is null)
        {
            return null;
        }

        var normalized = description.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new ValidationException("Currency must be a three-letter code.");
        }

        return normalized;
    }
}
