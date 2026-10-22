using System.ComponentModel.DataAnnotations;
using ShopCore.Application.Catalog;
using ShopCore.Application.Common;
using ShopCore.Domain.Enums;

namespace ShopCore.Application.Contracts;

public sealed record AdminProductSummary(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    decimal Price,
    string Currency,
    int Stock,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsActive,
    DateTime UpdatedAt,
    string RowVersion);

public sealed record AdminProductDetails(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int Stock,
    int ReservedQuantity,
    int AvailableQuantity,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion);

public sealed record AdminProductQueryRequest(
    [Range(1, int.MaxValue)] int? Page = null,
    [Range(1, CatalogPaging.MaxPageSize)] int? PageSize = null,
    Guid? CategoryId = null,
    [StringLength(100)] string? Search = null,
    [StringLength(20)] string? Sort = null,
    bool IncludeInactive = true);

public sealed record CategoryRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name);

public sealed record ProductRequest(
    [Required, StringLength(250, MinimumLength = 1)] string Name,
    [StringLength(4000)] string? Description,
    [PositiveAmount] decimal Price,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Range(0, int.MaxValue)] int Stock,
    [Required] Guid CategoryId);

public sealed record ProductUpdateRequest(
    [Required, StringLength(250, MinimumLength = 1)] string Name,
    [StringLength(4000)] string? Description,
    [PositiveAmount] decimal Price,
    [Required, StringLength(3, MinimumLength = 3)] string Currency,
    [Range(0, int.MaxValue)] int Stock,
    [Required] Guid CategoryId,
    [Required, StringLength(100, MinimumLength = 1)] string RowVersion);

public sealed record InventoryAdjustmentRequest(
    [Range(-1000000, 1000000)] int QuantityDelta,
    InventoryAdjustmentType Type,
    [Required, StringLength(500, MinimumLength = 1)] string Reason,
    [Required, StringLength(100, MinimumLength = 1)] string OperationReference);

public sealed record InventoryAdjustmentResult(
    Guid Id,
    Guid ProductId,
    int QuantityDelta,
    InventoryAdjustmentType Type,
    int Stock,
    int ReservedQuantity,
    DateTime CreatedAt,
    Guid ActorId);

public sealed record CategoryDeletionResult(Guid Id, bool IsActive, bool Deactivated);

public sealed record SearchReindexResult(int ProductsScanned, int TermsIndexed);

public interface IAdminCatalogService
{
    Task<CatalogPage<AdminProductSummary>> GetProductsAsync(AdminProductQueryRequest query, CancellationToken cancellationToken);
    Task<AdminProductDetails> GetProductAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminProductDetails> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken);
    Task<AdminProductDetails> UpdateProductAsync(Guid id, ProductUpdateRequest request, CancellationToken cancellationToken);
    Task DeactivateProductAsync(Guid id, CancellationToken cancellationToken);
    Task<InventoryAdjustmentResult> AdjustInventoryAsync(Guid productId, InventoryAdjustmentRequest request, CancellationToken cancellationToken);
    Task<CatalogPage<CategoryResponse>> GetCategoriesAsync(CategoryQueryRequest query, CancellationToken cancellationToken);
    Task<CategoryResponse> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<CategoryResponse> CreateCategoryAsync(CategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateCategoryAsync(Guid id, CategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryDeletionResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<SearchReindexResult> ReindexProductSearchAsync(CancellationToken cancellationToken);
}
