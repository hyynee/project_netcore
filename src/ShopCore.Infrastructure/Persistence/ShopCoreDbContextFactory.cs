using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopCore.Infrastructure.Persistence;

public sealed class ShopCoreDbContextFactory : IDesignTimeDbContextFactory<ShopCoreDbContext>
{
    public ShopCoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShopCoreDbContext>();
        var connectionString = ResolveConnectionString();

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.MigrationsAssembly(typeof(ShopCoreDbContext).Assembly.FullName));

        return new ShopCoreDbContext(optionsBuilder.Options, TimeProvider.System);
    }

    private static string ResolveConnectionString()
    {
        var fromApiConfiguration = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromApiConfiguration))
        {
            return fromApiConfiguration;
        }

        var fromTool = Environment.GetEnvironmentVariable("SHOPCORE_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(fromTool))
        {
            return fromTool;
        }

        var password = Environment.GetEnvironmentVariable("SQLSERVER_SA_PASSWORD");
        if (!string.IsNullOrWhiteSpace(password))
        {
            var host = Environment.GetEnvironmentVariable("SQLSERVER_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("SQLSERVER_PORT") ?? "1433";
            var database = Environment.GetEnvironmentVariable("SQLSERVER_DATABASE") ?? "ShopCore";
            return $"Server={host},{port};Database={database};User Id=sa;Password={password};TrustServerCertificate=True;Encrypt=False";
        }

        throw new InvalidOperationException(
            "A SQL Server connection string is required for EF Core commands. Set ConnectionStrings__DefaultConnection, " +
            "SHOPCORE_CONNECTION_STRING, or SQLSERVER_SA_PASSWORD (optionally with SQLSERVER_HOST, SQLSERVER_PORT and SQLSERVER_DATABASE).");
    }
}
