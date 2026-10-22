using ShopCore.Api.Exceptions;
using ShopCore.Application.Catalog;

namespace ShopCore.Api.Services;

internal static class CatalogQueryGuard
{
    private const int MaxSearchLength = 100;

    public static (int Page, int PageSize) ResolvePaging(int? page, int? pageSize)
    {
        var resolvedPage = page ?? 1;
        var resolvedPageSize = pageSize ?? CatalogPaging.DefaultPageSize;

        if (resolvedPage < 1)
        {
            throw new ValidationException("Page must be greater than or equal to 1.");
        }

        if (resolvedPageSize < 1 || resolvedPageSize > CatalogPaging.MaxPageSize)
        {
            throw new ValidationException(
                $"PageSize must be between 1 and {CatalogPaging.MaxPageSize}.");
        }

        return (resolvedPage, resolvedPageSize);
    }

    public static CatalogSortOption ParseSort(string? sort)
    {
        if (CatalogSort.TryParse(sort, out var option))
        {
            return option;
        }

        throw new ValidationException($"Sort must be one of: {CatalogSort.AllowedValues}.");
    }

    public static IReadOnlyList<string>? ResolveSearchTokens(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var trimmed = search.Trim();
        if (trimmed.Length > MaxSearchLength)
        {
            throw new ValidationException($"Search must not exceed {MaxSearchLength} characters.");
        }

        return ProductSearchTokenizer.Tokenize(trimmed);
    }

    public static int TotalPages(int totalCount, int pageSize) =>
        totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
}
