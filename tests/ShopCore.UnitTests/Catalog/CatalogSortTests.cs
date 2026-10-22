using ShopCore.Application.Catalog;

namespace ShopCore.UnitTests.Catalog;

public sealed class CatalogSortTests
{
    [Theory]
    [InlineData(null, CatalogSortOption.Newest)]
    [InlineData("", CatalogSortOption.Newest)]
    [InlineData("newest", CatalogSortOption.Newest)]
    [InlineData("name", CatalogSortOption.Name)]
    [InlineData("price", CatalogSortOption.PriceAscending)]
    [InlineData("-price", CatalogSortOption.PriceDescending)]
    [InlineData(" NAME ", CatalogSortOption.Name)]
    public void TryParse_AllowsListedValues_ReturnsExpectedOption(string? value, CatalogSortOption expected)
    {
        Assert.True(CatalogSort.TryParse(value, out var option));
        Assert.Equal(expected, option);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("stock")]
    [InlineData("reservedquantity")]
    [InlineData("price desc")]
    [InlineData("name; drop table Products")]
    public void TryParse_RejectsUnknownValues(string value)
    {
        Assert.False(CatalogSort.TryParse(value, out _));
    }

    [Fact]
    public void AllowedValues_DocumentsTheWhitelist()
    {
        Assert.Equal("newest, name, price, -price", CatalogSort.AllowedValues);
    }
}
