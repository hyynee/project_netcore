using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ShopCore.UnitTests.Catalog;

public sealed class CatalogSearchTests : IClassFixture<CatalogApiFixture>
{
    private readonly CatalogApiFixture _fixture;

    public CatalogSearchTests(CatalogApiFixture fixture)
    {
        _fixture = fixture;
    }

    [CatalogApiFact]
    public async Task SearchFindsProductByIndexedToken()
    {
        var marker = $"marker{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Aurora {marker} Speaker");
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync($"/api/v1/products?search={marker}");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task SearchRequiresEveryTokenToMatch()
    {
        var marker = $"both{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Nimbus {marker} Lamp");
        using var client = _fixture.CreateClient();

        using var matchResponse = await client.GetAsync($"/api/v1/products?search=nimbus%20{marker}");
        var match = await matchResponse.Content.ReadFromJsonAsync<ProductPageResponse>();
        using var missResponse = await client.GetAsync($"/api/v1/products?search=nimbus%20absent{marker}");
        var miss = await missResponse.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Contains(match!.Items, item => item.Id == product.Id);
        Assert.DoesNotContain(miss!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task SearchMatchesPrefixOfTheLastToken()
    {
        var marker = $"prefix{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Solstice {marker} Lantern");
        using var client = _fixture.CreateClient();
        var prefix = marker.Substring(0, marker.Length - 2);

        using var response = await client.GetAsync($"/api/v1/products?search={prefix}");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task SearchIgnoresPunctuationAndLetterCase()
    {
        var marker = $"punct{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Kite-{marker}-Kite");
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync($"/api/v1/products?search=KITE%20{marker}");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Contains(body!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task SearchWithoutIndexableTokenReturnsNoProducts()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/products?search=%25%25%25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [CatalogApiFact]
    public async Task SearchExcludesInactiveProducts()
    {
        var marker = $"inactive{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Umbra {marker} Clock", isActive: false);
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync($"/api/v1/products?search={marker}");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(body!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task SearchFindsVietnameseProductName()
    {
        var marker = $"vn{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Điện thoại {marker} Nokia");
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync($"/api/v1/products?search=điện%20{marker}");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task AdminListSearchUsesTheSameIndex()
    {
        var marker = $"admin{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Vertex {marker} Module");
        using var client = _fixture.CreateAdminClient();

        using var response = await client.GetAsync($"/api/v1/admin/products?search={marker}");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task RenamedProductIsIndexedUnderTheNewNameOnly()
    {
        var category = await _fixture.SeedCategoryAsync();
        var oldMarker = $"old{Guid.NewGuid():N}";
        var newMarker = $"new{Guid.NewGuid():N}";
        using var client = _fixture.CreateAdminClient();

        using var createResponse = await client.PostAsJsonAsync("/api/v1/admin/products", new
        {
            name = $"Legacy {oldMarker} Device",
            description = "Created by the phase 03 search tests.",
            price = 100_000m,
            currency = "VND",
            stock = 5,
            categoryId = category.Id
        });
        var created = await createResponse.Content.ReadFromJsonAsync<AdminProductResponse>();
        Assert.NotNull(created);

        using var updateResponse = await client.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", new
        {
            name = $"Renamed {newMarker} Device",
            description = "Created by the phase 03 search tests.",
            price = 100_000m,
            currency = "VND",
            stock = 5,
            categoryId = category.Id,
            rowVersion = created.RowVersion
        });
        updateResponse.EnsureSuccessStatusCode();

        using var staleSearch = await client.GetAsync($"/api/v1/products?search={oldMarker}");
        var staleBody = await staleSearch.Content.ReadFromJsonAsync<ProductPageResponse>();
        using var freshSearch = await client.GetAsync($"/api/v1/products?search={newMarker}");
        var freshBody = await freshSearch.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.DoesNotContain(staleBody!.Items, item => item.Id == created.Id);
        Assert.Contains(freshBody!.Items, item => item.Id == created.Id);
    }

    [CatalogApiFact]
    public async Task ReindexRebuildsTheSearchIndexForExistingProducts()
    {
        var marker = $"reindex{Guid.NewGuid():N}";
        var product = await _fixture.SeedNamedProductAsync($"Helio {marker} Module");
        using var client = _fixture.CreateAdminClient();

        await using (var db = _fixture.CreateContext())
        {
            await db.ProductSearchTerms.Where(term => term.ProductId == product.Id).ExecuteDeleteAsync();
        }

        using var staleSearch = await client.GetAsync($"/api/v1/products?search={marker}");
        var staleBody = await staleSearch.Content.ReadFromJsonAsync<ProductPageResponse>();
        Assert.DoesNotContain(staleBody!.Items, item => item.Id == product.Id);

        using var reindexResponse = await client.PostAsync("/api/v1/admin/search/reindex", content: null);
        var reindex = await reindexResponse.Content.ReadFromJsonAsync<SearchReindexResponse>();

        Assert.Equal(HttpStatusCode.OK, reindexResponse.StatusCode);
        Assert.NotNull(reindex);
        Assert.True(reindex.ProductsScanned >= 1);
        Assert.True(reindex.TermsIndexed >= 1);

        using var freshSearch = await client.GetAsync($"/api/v1/products?search={marker}");
        var freshBody = await freshSearch.Content.ReadFromJsonAsync<ProductPageResponse>();
        Assert.Contains(freshBody!.Items, item => item.Id == product.Id);
    }

    [CatalogApiFact]
    public async Task SearchResponseUsesTheDocumentedJsonShape()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/products?page=1&pageSize=1");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(
            ["items", "page", "pageSize", "totalCount", "totalPages"],
            root.EnumerateObject().Select(property => property.Name).ToArray());

        var firstItem = root.GetProperty("items").EnumerateArray().First();
        Assert.Equal(
            ["id", "categoryId", "categoryName", "name", "description", "price", "currency", "availableQuantity"],
            firstItem.EnumerateObject().Select(property => property.Name).ToArray());
    }

    private sealed record ProductPageResponse(
        List<ProductItem> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private sealed record ProductItem(Guid Id, string Name, decimal Price, string Currency, int AvailableQuantity);

    private sealed record AdminProductResponse(
        Guid Id,
        string Name,
        decimal Price,
        string Currency,
        int Stock,
        int ReservedQuantity,
        bool IsActive,
        string RowVersion);

    private sealed record SearchReindexResponse(int ProductsScanned, int TermsIndexed);
}
