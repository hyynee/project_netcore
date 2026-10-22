using Microsoft.EntityFrameworkCore;
using ShopCore.Domain.Entities;

namespace ShopCore.Infrastructure.Persistence;

/// <summary>
/// Creates the sample catalog rows used by local development and automated tests.
/// It never runs in staging or production, so the database schema and data stay free
/// of demo rows outside developer machines.
/// </summary>
public static class CatalogSampleSeeder
{
    public static async Task SeedAsync(ShopCoreDbContext db, CancellationToken cancellationToken)
    {
        var categoryExists = await db.Categories
            .AnyAsync(category => category.Id == ShopCoreDbContext.SampleCategoryId, cancellationToken);
        if (!categoryExists)
        {
            db.Categories.Add(new Category
            {
                Id = ShopCoreDbContext.SampleCategoryId,
                Name = "Sample Category",
                IsActive = true
            });
        }

        var productExists = await db.Products
            .AnyAsync(product => product.Id == ShopCoreDbContext.SampleProductId, cancellationToken);
        if (!productExists)
        {
            db.Products.Add(new Product
            {
                Id = ShopCoreDbContext.SampleProductId,
                CategoryId = ShopCoreDbContext.SampleCategoryId,
                Name = "Sample Product",
                Description = "Sample product used by the phase 03 vertical slice.",
                Price = 100_000m,
                Currency = "VND",
                Stock = 25,
                ReservedQuantity = 0,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
