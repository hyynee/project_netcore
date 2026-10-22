using Microsoft.EntityFrameworkCore;
using ShopCore.Domain.Entities;

namespace ShopCore.Api.Services;

/// <summary>
/// Builds the product search predicate on top of the product search index so that
/// catalog search is answered by an index seek on the term rows instead of a name scan.
/// </summary>
internal static class CatalogSearchFilter
{
    public static IQueryable<Product> Apply(
        IQueryable<Product> products,
        IQueryable<ProductSearchTerm> searchTerms,
        IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return products.Where(product => false);
        }

        var lastToken = tokens[^1];
        var exactTokens = tokens.Take(tokens.Count - 1).ToArray();

        IQueryable<Guid> matchingProductIds = searchTerms
            .Where(term => term.Term.StartsWith(lastToken))
            .Select(term => term.ProductId)
            .Distinct();

        if (exactTokens.Length > 0)
        {
            var exactProductIds = searchTerms
                .Where(term => exactTokens.Contains(term.Term))
                .GroupBy(term => term.ProductId)
                .Where(group => group.Count() == exactTokens.Length)
                .Select(group => group.Key);

            matchingProductIds = exactProductIds.Intersect(matchingProductIds);
        }

        return products.Where(product => matchingProductIds.Contains(product.Id));
    }
}
