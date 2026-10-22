using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopCore.Api.Controllers;
using ShopCore.Application.Contracts;

namespace ShopCore.UnitTests.Catalog;

public sealed class AdminEndpointPolicyTests
{
    public static TheoryData<Type> AdminControllers =>
        new() { typeof(AdminProductsController), typeof(AdminCategoriesController), typeof(AdminSearchController) };

    [Theory]
    [MemberData(nameof(AdminControllers))]
    public void EveryAdminControllerRequiresTheAdminRole(Type controllerType)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(ShopPolicies.AdminOnly, authorize.Policy);
    }

    [Theory]
    [MemberData(nameof(AdminControllers))]
    public void EveryAdminControllerUsesTheAdminRateLimitPolicy(Type controllerType)
    {
        var policy = controllerType.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

        Assert.Equal("admin", policy);
    }

    [Fact]
    public void ThePublicCatalogUsesTheApiRateLimitPolicy()
    {
        var policy = typeof(ProductsController).GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

        Assert.Equal("api", policy);
    }

    [Fact]
    public void ThePublicCatalogIsNotRoleProtected()
    {
        var authorize = typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.True(authorize is null || string.IsNullOrEmpty(authorize.Policy));
    }
}
