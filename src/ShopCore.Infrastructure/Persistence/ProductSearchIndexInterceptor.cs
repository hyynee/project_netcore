using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShopCore.Application.Catalog;
using ShopCore.Domain.Entities;

namespace ShopCore.Infrastructure.Persistence;

/// <summary>
/// Keeps the product search index in sync with the product name so that catalog search
/// can use an index seek instead of a scan over every product name.
/// </summary>
public sealed class ProductSearchIndexInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SyncSearchIndex(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SyncSearchIndex(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SyncSearchIndex(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var changedProducts = context.ChangeTracker.Entries<Product>()
            .Where(entry => entry.State == EntityState.Added ||
                            (entry.State == EntityState.Modified && IsNameChanged(entry)))
            .ToList();

        if (changedProducts.Count == 0)
        {
            return;
        }

        var productIds = changedProducts.Select(entry => entry.Entity.Id).ToList();
        var existingTerms = context.Set<ProductSearchTerm>()
            .Where(term => productIds.Contains(term.ProductId))
            .ToList();

        context.Set<ProductSearchTerm>().RemoveRange(existingTerms);

        foreach (var entry in changedProducts)
        {
            foreach (var term in ProductSearchTokenizer.Tokenize(entry.Entity.Name))
            {
                context.Set<ProductSearchTerm>().Add(new ProductSearchTerm
                {
                    ProductId = entry.Entity.Id,
                    Term = term
                });
            }
        }
    }

    private static bool IsNameChanged(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Product> entry) =>
        !string.Equals(entry.OriginalValues[nameof(Product.Name)] as string, entry.Entity.Name, StringComparison.Ordinal);
}
