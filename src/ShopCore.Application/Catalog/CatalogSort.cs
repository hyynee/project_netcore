namespace ShopCore.Application.Catalog;

public enum CatalogSortOption
{
    Newest,
    Name,
    PriceAscending,
    PriceDescending
}

public static class CatalogSort
{
    public const string Newest = "newest";
    public const string Name = "name";
    public const string Price = "price";
    public const string PriceDescending = "-price";

    public const string AllowedValues = Newest + ", " + Name + ", " + Price + ", " + PriceDescending;

    public static bool TryParse(string? value, out CatalogSortOption option)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null:
            case "":
            case Newest:
                option = CatalogSortOption.Newest;
                return true;
            case Name:
                option = CatalogSortOption.Name;
                return true;
            case Price:
                option = CatalogSortOption.PriceAscending;
                return true;
            case PriceDescending:
                option = CatalogSortOption.PriceDescending;
                return true;
            default:
                option = CatalogSortOption.Newest;
                return false;
        }
    }
}
