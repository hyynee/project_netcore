using System.Text;

namespace ShopCore.Application.Catalog;

/// <summary>
/// Splits product names into the normalized terms stored in the product search index.
/// The rules live here so the write path (indexing) and the read path (querying) stay identical.
/// </summary>
public static class ProductSearchTokenizer
{
    public const int MinimumTermLength = 2;

    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var builder = new StringBuilder();

        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                builder.Append(rune.ToString().ToLowerInvariant());
                continue;
            }

            Flush(builder, terms, seen);
        }

        Flush(builder, terms, seen);

        return terms;
    }

    private static void Flush(StringBuilder builder, List<string> terms, HashSet<string> seen)
    {
        if (builder.Length == 0)
        {
            return;
        }

        var term = builder.ToString();
        builder.Clear();

        if (term.Length < MinimumTermLength || !seen.Add(term))
        {
            return;
        }

        terms.Add(term);
    }
}
