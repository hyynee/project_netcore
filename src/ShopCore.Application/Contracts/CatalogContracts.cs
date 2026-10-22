using System.ComponentModel.DataAnnotations;
using ShopCore.Application.Catalog;

namespace ShopCore.Application.Contracts;

public sealed record CatalogPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProductSummary(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int AvailableQuantity);

public sealed record ProductDetails(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int AvailableQuantity);

public sealed record CategoryResponse(Guid Id, string Name, bool IsActive);

public sealed record ProductQueryRequest(
    [Range(1, int.MaxValue)] int? Page = null,
    [Range(1, CatalogPaging.MaxPageSize)] int? PageSize = null,
    Guid? CategoryId = null,
    [StringLength(100)] string? Search = null,
    [StringLength(20)] string? Sort = null);

public sealed record CategoryQueryRequest(
    [Range(1, int.MaxValue)] int? Page = null,
    [Range(1, CatalogPaging.MaxPageSize)] int? PageSize = null);

public interface ICatalogService
{
    Task<CatalogPage<ProductSummary>> GetProductsAsync(ProductQueryRequest query, CancellationToken cancellationToken);
    Task<ProductDetails> GetPublicProductAsync(Guid id, CancellationToken cancellationToken);
    Task<CatalogPage<CategoryResponse>> GetActiveCategoriesAsync(CategoryQueryRequest query, CancellationToken cancellationToken);
}
