using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ShopCore.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddShopCorePersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool addHealthCheck = true)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (environment.IsStaging() || environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection must be configured outside Development and Testing.");
            }

            return services;
        }

        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<ShopCoreDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sqlOptions.MigrationsAssembly(typeof(ShopCoreDbContext).Assembly.FullName);
            }));

        if (addHealthCheck)
        {
            services.AddHealthChecks().AddDbContextCheck<ShopCoreDbContext>("database");
        }

        return services;
    }
}
