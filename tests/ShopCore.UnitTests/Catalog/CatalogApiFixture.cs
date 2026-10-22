using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopCore.Application.Contracts;
using ShopCore.Domain.Entities;
using ShopCore.Infrastructure.Persistence;
using ShopCore.UnitTests.Support;
using Testcontainers.MsSql;

namespace ShopCore.UnitTests.Catalog;

public sealed class CatalogApiFixture : IAsyncLifetime
{
    private const string ContainerPassword = "Your_strong_password123!";
    private const string SigningKey = "test-signing-key-with-at-least-32-characters";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword(ContainerPassword)
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public const string UserPassword = "CatalogPassw0rd!2026";
    public string AdminEmail => "admin@catalog.test";
    public string CustomerEmail => "customer@catalog.test";
    public string AdminToken { get; private set; } = string.Empty;
    public string CustomerToken { get; private set; } = string.Empty;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        await _container.StartAsync();

        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("Jwt:SigningKey", SigningKey);
            builder.UseSetting("Jwt:Issuer", "ShopCore");
            builder.UseSetting("Jwt:Audience", "ShopCore.Api");
        });

        await SeedUsersAsync();
        AdminToken = await LoginAsync(AdminEmail);
        CustomerToken = await LoginAsync(CustomerEmail);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (DockerAvailability.IsAvailable)
        {
            await _container.DisposeAsync();
        }
    }

    public ShopCoreDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopCoreDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ShopCoreDbContext(options, TimeProvider.System);
    }

    public HttpClient CreateClient() =>
        (_factory ?? throw new InvalidOperationException("The catalog API factory is not initialized."))
        .CreateClient();

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
        return client;
    }

    public HttpClient CreateCustomerClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CustomerToken);
        return client;
    }

    public async Task<Category> SeedCategoryAsync()
    {
        await using var db = CreateContext();
        var category = new Category { Name = $"Test-{Guid.NewGuid():N}" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task<Product> SeedProductAsync(bool isActive = true, int stock = 10)
    {
        await using var db = CreateContext();
        var category = new Category { Name = $"Test-{Guid.NewGuid():N}" };
        var product = new Product
        {
            Category = category,
            Name = $"Test product {Guid.NewGuid():N}",
            Price = 100_000,
            Currency = "VND",
            Stock = stock,
            IsActive = isActive
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> SeedNamedProductAsync(string name, bool isActive = true, int stock = 10)
    {
        await using var db = CreateContext();
        var category = new Category { Name = $"Test-{Guid.NewGuid():N}" };
        var product = new Product
        {
            Category = category,
            Name = name,
            Price = 100_000,
            Currency = "VND",
            Stock = stock,
            IsActive = isActive
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    private async Task SeedUsersAsync()
    {
        var factory = _factory ?? throw new InvalidOperationException("The catalog API factory is not initialized.");
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { ShopRoles.Admin, ShopRoles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, ShopRoles.Admin);
        await EnsureUserAsync(userManager, CustomerEmail, ShopRoles.Customer);
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };
            var result = await userManager.CreateAsync(user, UserPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed the catalog test user {email}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private async Task<string> LoginAsync(string email)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password = UserPassword });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth?.AccessToken ?? throw new InvalidOperationException("The login response did not contain an access token.");
    }
}
