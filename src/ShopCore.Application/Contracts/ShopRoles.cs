namespace ShopCore.Application.Contracts;

public static class ShopRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

public static class ShopPolicies
{
    public const string AdminOnly = "AdminOnly";
}
