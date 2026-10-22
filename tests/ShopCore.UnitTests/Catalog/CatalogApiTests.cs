using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ShopCore.Domain.Entities;
using ShopCore.Domain.Enums;

namespace ShopCore.UnitTests.Catalog;

public sealed class CatalogApiTests : IClassFixture<CatalogApiFixture>
{
    private readonly CatalogApiFixture _fixture;

    public CatalogApiTests(CatalogApiFixture fixture)
    {
        _fixture = fixture;
    }

    [CatalogApiFact]
    public async Task GuestCanListActiveProductsWithPaginationMetadata()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/products?page=1&pageSize=5");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(5, body.PageSize);
        Assert.True(body.TotalCount >= 1);
        Assert.Equal(
            body.TotalCount == 0 ? 0 : (int)Math.Ceiling(body.TotalCount / (double)body.PageSize),
            body.TotalPages);
        Assert.All(body.Items, item => Assert.True(item.AvailableQuantity >= 0));
    }

    [CatalogApiFact]
    public async Task GuestProductProjectionExposesAvailableQuantityOnly()
    {
        Guid productId;
        await using (var db = _fixture.CreateContext())
        {
            var category = new Category { Name = $"Projection-{Guid.NewGuid():N}" };
            var product = new Product
            {
                Category = category,
                Name = $"Projection product {Guid.NewGuid():N}",
                Price = 100_000m,
                Currency = "VND",
                Stock = 10,
                ReservedQuantity = 3,
                IsActive = true
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            productId = product.Id;
        }

        using var client = _fixture.CreateClient();

        using (var listResponse = await client.GetAsync("/api/v1/products?pageSize=100"))
        {
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
            var item = list.RootElement.GetProperty("items").EnumerateArray()
                .Single(element => element.GetProperty("id").GetGuid() == productId);

            Assert.Equal(7, item.GetProperty("availableQuantity").GetInt32());
            AssertNoInventoryFields(item);
        }

        using var detailResponse = await client.GetAsync($"/api/v1/products/{productId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        using var detail = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        Assert.Equal(7, detail.RootElement.GetProperty("availableQuantity").GetInt32());
        AssertNoInventoryFields(detail.RootElement);
    }

    private static void AssertNoInventoryFields(JsonElement element)
    {
        var names = element.EnumerateObject().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(names, name => string.Equals(name, "stock", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => string.Equals(name, "reservedQuantity", StringComparison.OrdinalIgnoreCase));
    }

    [CatalogApiFact]
    public async Task GuestListExcludesInactiveProducts()
    {
        var inactive = await _fixture.SeedProductAsync(isActive: false);
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/products?pageSize=100");
        var body = await response.Content.ReadFromJsonAsync<ProductPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.DoesNotContain(body.Items, item => item.Id == inactive.Id);
    }

    [CatalogApiFact]
    public async Task GuestCannotReadInactiveProduct()
    {
        var inactive = await _fixture.SeedProductAsync(isActive: false);
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync($"/api/v1/products/{inactive.Id}");
        var body = await response.Content.ReadFromJsonAsync<ProblemResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(404, body?.Status);
        Assert.False(string.IsNullOrWhiteSpace(body?.TraceId));
    }

    [CatalogApiFact]
    public async Task GuestListRejectsPageSizeAboveMaximum()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/products?pageSize=1000");
        var body = await response.Content.ReadFromJsonAsync<ProblemResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(body?.Errors);
        Assert.Contains("PageSize", body.Errors.Keys);
    }

    [CatalogApiFact]
    public async Task GuestListRejectsUnknownSort()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/products?sort=stock");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task GuestListReturnsOnlyActiveCategories()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/api/v1/categories?pageSize=100");
        var body = await response.Content.ReadFromJsonAsync<CategoryPageResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.All(body.Items, item => Assert.True(item.IsActive));
        Assert.Contains(body.Items, item => item.Id == category.Id);
    }

    [CatalogApiFact]
    public async Task AdminCreatesProductAndReturnsLocation()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        using var response = await client.PostAsJsonAsync("/api/v1/admin/products", new
        {
            name = $"Admin created {Guid.NewGuid():N}",
            description = "Created by the phase 03 API tests.",
            price = 250_000m,
            currency = "vnd",
            stock = 12,
            categoryId = category.Id
        });
        var body = await response.Content.ReadFromJsonAsync<AdminProductResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("VND", body.Currency);
        Assert.Equal(12, body.Stock);
        Assert.NotNull(response.Headers.Location);
        Assert.False(string.IsNullOrWhiteSpace(body.RowVersion));
    }

    [CatalogApiFact]
    public async Task CustomerCannotManageTheCatalog()
    {
        using var client = _fixture.CreateCustomerClient();

        using var response = await client.GetAsync("/api/v1/admin/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task AnonymousCannotManageTheCatalog()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/admin/categories", new { name = "Nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task AdminUpdateWithStaleRowVersionReturnsConflict()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(client, category.Id, 10);
        var update = new
        {
            name = created.Name,
            description = "First update.",
            price = 300_000m,
            currency = "VND",
            stock = 11,
            categoryId = category.Id,
            rowVersion = created.RowVersion
        };

        using var first = await client.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", update);
        using var second = await client.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", update);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [CatalogApiFact]
    public async Task ParallelAdminUpdatesKeepExactlyOneWinner()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var firstClient = _fixture.CreateAdminClient();
        using var secondClient = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(firstClient, category.Id, 10);
        object Update(int stock, string description) => new
        {
            name = created.Name,
            description,
            price = 300_000m,
            currency = "VND",
            stock,
            categoryId = category.Id,
            rowVersion = created.RowVersion
        };

        var responses = await Task.WhenAll(
            firstClient.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", Update(11, "Parallel A")),
            secondClient.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", Update(12, "Parallel B")));

        try
        {
            var statuses = responses.Select(response => response.StatusCode).OrderBy(status => status).ToArray();

            Assert.Equal(HttpStatusCode.OK, statuses[0]);
            Assert.Equal(HttpStatusCode.Conflict, statuses[1]);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [CatalogApiFact]
    public async Task AdminCreateRejectsFractionalPrice()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        using var response = await client.PostAsJsonAsync("/api/v1/admin/products", new
        {
            name = $"Fractional {Guid.NewGuid():N}",
            description = "Fractional prices are rejected.",
            price = 100_000.5m,
            currency = "VND",
            stock = 1,
            categoryId = category.Id
        });
        var body = await response.Content.ReadFromJsonAsync<ProblemResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body?.Errors);
        Assert.Contains("Price", body.Errors.Keys);
    }

    [CatalogApiFact]
    public async Task AdminUpdateRejectsFractionalPrice()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(client, category.Id, 10);

        using var response = await client.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", new
        {
            name = created.Name,
            description = "Fractional prices are rejected.",
            price = 200_000.25m,
            currency = "VND",
            stock = 10,
            categoryId = category.Id,
            rowVersion = created.RowVersion
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task AdminWritesStoreTheAuditActor()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(client, category.Id, 10);

        await using (var db = _fixture.CreateContext())
        {
            var admin = await db.Users.AsNoTracking().SingleAsync(x => x.Email == _fixture.AdminEmail);
            var product = await db.Products.AsNoTracking().SingleAsync(x => x.Id == created.Id);

            Assert.Equal(admin.Id, product.CreatedBy);
            Assert.Equal(admin.Id, product.UpdatedBy);
        }

        var current = await GetAdminProductAsync(client, created.Id);
        using var updateResponse = await client.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", new
        {
            name = current.Name,
            description = "Updated by the audit test.",
            price = 100_000m,
            currency = "VND",
            stock = 12,
            categoryId = category.Id,
            rowVersion = current.RowVersion
        });
        updateResponse.EnsureSuccessStatusCode();

        await using (var db = _fixture.CreateContext())
        {
            var admin = await db.Users.AsNoTracking().SingleAsync(x => x.Email == _fixture.AdminEmail);
            var product = await db.Products.AsNoTracking().SingleAsync(x => x.Id == created.Id);

            Assert.Equal(admin.Id, product.CreatedBy);
            Assert.Equal(admin.Id, product.UpdatedBy);
        }
    }

    [CatalogApiFact]
    public async Task AdminCannotSetStockBelowReservedQuantity()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(client, category.Id, 10);

        await using (var db = _fixture.CreateContext())
        {
            var product = await db.Products.SingleAsync(x => x.Id == created.Id);
            product.Stock = 10;
            product.ReservedQuantity = 5;
            await db.SaveChangesAsync();
        }

        var current = await GetAdminProductAsync(client, created.Id);
        using var response = await client.PutAsJsonAsync($"/api/v1/admin/products/{created.Id}", new
        {
            name = current.Name,
            description = "Below reserved.",
            price = 100_000m,
            currency = "VND",
            stock = 3,
            categoryId = category.Id,
            rowVersion = current.RowVersion
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task AdminCannotAssignUnknownCategory()
    {
        using var client = _fixture.CreateAdminClient();

        using var response = await client.PostAsJsonAsync("/api/v1/admin/products", new
        {
            name = $"Orphan {Guid.NewGuid():N}",
            description = "Invalid category.",
            price = 100_000m,
            currency = "VND",
            stock = 1,
            categoryId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task InventoryAdjustmentUpdatesStockAndWritesAudit()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(client, category.Id, 10);
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/admin/products/{created.Id}/inventory-adjustments",
            new
            {
                quantityDelta = 5,
                type = "Restock",
                reason = "Goods received.",
                operationReference = $"ref-{Guid.NewGuid():N}"
            });
        var body = await response.Content.ReadFromJsonAsync<InventoryAdjustmentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(15, body.Stock);
        Assert.Equal(0, body.ReservedQuantity);

        await using var db = _fixture.CreateContext();
        var adjustment = await db.InventoryAdjustments.AsNoTracking()
            .SingleAsync(x => x.ProductId == created.Id);
        var admin = await db.Users.AsNoTracking().SingleAsync(x => x.Email == _fixture.AdminEmail);

        Assert.Equal(5, adjustment.QuantityDelta);
        Assert.Equal(InventoryAdjustmentType.Restock, adjustment.Type);
        Assert.Equal("Goods received.", adjustment.Reason);
        Assert.Equal(admin.Id, adjustment.CreatedBy);
        Assert.NotEqual(default, adjustment.CreatedAt);
    }

    [CatalogApiFact]
    public async Task InventoryAdjustmentCannotReduceStockBelowReservedQuantity()
    {
        var category = await _fixture.SeedCategoryAsync();
        using var client = _fixture.CreateAdminClient();

        var created = await CreateProductAsync(client, category.Id, 10);

        await using (var db = _fixture.CreateContext())
        {
            var product = await db.Products.SingleAsync(x => x.Id == created.Id);
            product.Stock = 10;
            product.ReservedQuantity = 5;
            await db.SaveChangesAsync();
        }

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/admin/products/{created.Id}/inventory-adjustments",
            new
            {
                quantityDelta = -8,
                type = "Damage",
                reason = "Broken goods.",
                operationReference = $"ref-{Guid.NewGuid():N}"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [CatalogApiFact]
    public async Task CategoryWithProductsIsDeactivatedInsteadOfDeleted()
    {
        using var client = _fixture.CreateAdminClient();

        var categoryName = $"Category-{Guid.NewGuid():N}";
        using var createResponse = await client.PostAsJsonAsync("/api/v1/admin/categories", new { name = categoryName });
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryItem>();
        Assert.NotNull(category);

        var created = await CreateProductAsync(client, category.Id, 5);

        using var deleteResponse = await client.DeleteAsync($"/api/v1/admin/categories/{category.Id}");
        var deletion = await deleteResponse.Content.ReadFromJsonAsync<CategoryDeletionResponse>();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.NotNull(deletion);
        Assert.True(deletion.Deactivated);
        Assert.False(deletion.IsActive);

        using var listResponse = await client.GetAsync("/api/v1/admin/categories?pageSize=100");
        var list = await listResponse.Content.ReadFromJsonAsync<CategoryPageResponse>();
        Assert.NotNull(list);
        Assert.Contains(list.Items, item => item.Id == category.Id && !item.IsActive);

        using var productResponse = await client.GetAsync($"/api/v1/admin/products/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, productResponse.StatusCode);
    }

    [CatalogApiFact]
    public async Task CategoryWithoutProductsIsDeleted()
    {
        using var client = _fixture.CreateAdminClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/admin/categories",
            new { name = $"Disposable-{Guid.NewGuid():N}" });
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryItem>();
        Assert.NotNull(category);

        using var deleteResponse = await client.DeleteAsync($"/api/v1/admin/categories/{category.Id}");
        var deletion = await deleteResponse.Content.ReadFromJsonAsync<CategoryDeletionResponse>();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.NotNull(deletion);
        Assert.False(deletion.Deactivated);

        using var getResponse = await client.GetAsync($"/api/v1/admin/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private static async Task<AdminProductResponse> CreateProductAsync(
        HttpClient client,
        Guid categoryId,
        int stock)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/admin/products", new
        {
            name = $"Product-{Guid.NewGuid():N}",
            description = "Created by the phase 03 API tests.",
            price = 100_000m,
            currency = "VND",
            stock,
            categoryId
        });
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<AdminProductResponse>();
        return product ?? throw new InvalidOperationException("The product response was empty.");
    }

    private static async Task<AdminProductResponse> GetAdminProductAsync(HttpClient client, Guid id)
    {
        using var response = await client.GetAsync($"/api/v1/admin/products/{id}");
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<AdminProductResponse>();
        return product ?? throw new InvalidOperationException("The product response was empty.");
    }

    private sealed record ProductPageResponse(
        List<ProductItem> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private sealed record ProductItem(Guid Id, string Name, decimal Price, string Currency, int AvailableQuantity);

    private sealed record CategoryPageResponse(
        List<CategoryItem> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private sealed record CategoryItem(Guid Id, string Name, bool IsActive);

    private sealed record CategoryDeletionResponse(Guid Id, bool IsActive, bool Deactivated);

    private sealed record AdminProductResponse(
        Guid Id,
        string Name,
        decimal Price,
        string Currency,
        int Stock,
        int ReservedQuantity,
        bool IsActive,
        string RowVersion);

    private sealed record InventoryAdjustmentResponse(
        Guid Id,
        Guid ProductId,
        int QuantityDelta,
        string Type,
        int Stock,
        int ReservedQuantity,
        DateTime CreatedAt,
        Guid ActorId);

    private sealed record ProblemResponse(
        int? Status,
        string? Title,
        string? TraceId,
        Dictionary<string, string[]>? Errors);
}
