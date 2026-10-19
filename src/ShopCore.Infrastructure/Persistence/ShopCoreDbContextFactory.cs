using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShopCore.Infrastructure.Persistence;

public sealed class ShopCoreDbContextFactory : IDesignTimeDbContextFactory<ShopCoreDbContext>
{
    public ShopCoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ShopCoreDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("SHOPCORE_CONNECTION_STRING")
            ?? "Server=localhost,1433;Database=ShopCore;User Id=sa;Password=Your_strong_password123!;TrustServerCertificate=True;Encrypt=False";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.MigrationsAssembly(typeof(ShopCoreDbContext).Assembly.FullName));

        return new ShopCoreDbContext(optionsBuilder.Options, TimeProvider.System);
    }
}
