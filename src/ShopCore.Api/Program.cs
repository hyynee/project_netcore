using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ShopCore.Api.Configuration;
using ShopCore.Api.Infrastructure;
using ShopCore.Api.Identity;
using ShopCore.Application.Contracts;
using ShopCore.Domain.Entities;
using ShopCore.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

if ((builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")) &&
    string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SigningKey"]))
{
    builder.Configuration["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services
    .AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();
if (!builder.Environment.IsEnvironment("Testing") &&
    string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is required to run ShopCore.Api. " +
        "Start SQL Server with 'docker compose up -d' and export the connection string before running the API.");
}

builder.Services.AddShopCorePersistence(builder.Configuration, builder.Environment);
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ShopCoreDbContext>()
    .AddSignInManager();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IUserProfileService, ShopCore.Api.Services.UserProfileService>();
builder.Services.AddScoped<ICatalogService, ShopCore.Api.Services.CatalogService>();
builder.Services.AddScoped<IAdminCatalogService, ShopCore.Api.Services.AdminCatalogService>();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ShopCore API",
        Version = "v1",
        Description = "ShopCore e-commerce backend API."
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ShopPolicies.AdminOnly, policy => policy.RequireRole(ShopRoles.Admin));
});
var proxyOptions = builder.Configuration.GetSection(ProxyOptions.SectionName).Get<ProxyOptions>() ?? new ProxyOptions();
proxyOptions.KnownProxies = ProxyOptions.ReadKnownEntries(builder.Configuration, nameof(ProxyOptions.KnownProxies));
proxyOptions.KnownNetworks = ProxyOptions.ReadKnownEntries(builder.Configuration, nameof(ProxyOptions.KnownNetworks));
if (proxyOptions.TrustForwardedHeaders &&
    proxyOptions.KnownProxies.Length == 0 &&
    proxyOptions.KnownNetworks.Length == 0)
{
    throw new InvalidOperationException(
        "Proxy:TrustForwardedHeaders requires at least one Proxy:KnownProxies or Proxy:KnownNetworks entry. " +
        "Trusting forwarded headers without a known proxy lets a client spoof its own IP address.");
}

builder.Services.Configure<ProxyOptions>(options =>
{
    options.TrustForwardedHeaders = proxyOptions.TrustForwardedHeaders;
    options.KnownProxies = proxyOptions.KnownProxies;
    options.KnownNetworks = proxyOptions.KnownNetworks;
});
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var invariant = CultureInfo.InvariantCulture;
    options.DefaultRequestCulture = new RequestCulture(invariant);
    options.SupportedCultures = [invariant];
    options.SupportedUICultures = [invariant];
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Type = "https://httpstatuses.com/429",
                Title = "Too many requests.",
                Status = StatusCodes.Status429TooManyRequests,
                Instance = context.HttpContext.Request.Path,
                Extensions = { ["traceId"] = context.HttpContext.TraceIdentifier }
            },
            cancellationToken);
    };
    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("auth-register", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{context.Connection.RemoteIpAddress}|{context.Request.Headers["X-Auth-Account"]}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth-refresh", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth-changepw", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{context.Connection.RemoteIpAddress}|{context.Request.Headers["X-Auth-Account"]}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth-logoutall", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("admin", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}|{context.User.Identity?.Name ?? "unknown"}",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

if (!builder.Environment.IsDevelopment() &&
    !builder.Environment.IsEnvironment("Testing") &&
    corsOptions.AllowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "At least one production CORS origin must be configured.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .WithHeaders("Authorization", "Content-Type", "Accept", "Idempotency-Key", "Stripe-Signature")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
    });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024;
});
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 1 * 1024 * 1024);

var app = builder.Build();

if (!app.Environment.IsProduction() &&
    !app.Environment.IsEnvironment("Testing") &&
    !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { ShopRoles.Customer, ShopRoles.Admin })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not seed role {role}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            }
        }

    }

    if (app.Environment.IsDevelopment() &&
        !string.IsNullOrWhiteSpace(builder.Configuration["Identity:DevelopmentAdminEmail"]) &&
        !string.IsNullOrWhiteSpace(builder.Configuration["Identity:DevelopmentAdminPassword"]))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = builder.Configuration["Identity:DevelopmentAdminEmail"]!;
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email };
            var createResult = await userManager.CreateAsync(
                admin,
                builder.Configuration["Identity:DevelopmentAdminPassword"]!);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException("Could not seed the development admin.");
            }
        }

        if (!await userManager.IsInRoleAsync(admin, ShopRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, ShopRoles.Admin);
        }
    }
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    using var sampleScope = app.Services.CreateScope();
    var sampleDb = sampleScope.ServiceProvider.GetService<ShopCoreDbContext>();
    if (sampleDb is not null)
    {
        await CatalogSampleSeeder.SeedAsync(sampleDb, CancellationToken.None);
    }
}

app.UseMiddleware<KnownProxyForwardedHeadersMiddleware>();
app.UseRequestLocalization();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopCore API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "ShopCore API";
    });
}

app.MapHealthChecks("/health/live", new() { Predicate = _ => false })
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapHealthChecks("/health/ready")
    .AllowAnonymous()
    .DisableRateLimiting();
app.MapControllers();

app.Run();

public partial class Program;
