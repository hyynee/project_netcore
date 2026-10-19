using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ShopCore.UnitTests;

public sealed class ApiPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiPipelineTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing")).CreateClient();
    }

    [Fact]
    public async Task LiveHealthEndpointReturnsOk()
    {
        using var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledExceptionReturnsProblemDetailsWithoutStackTrace()
    {
        using var response = await _client.GetAsync("/api/v1/diagnostics/error");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(body);
        Assert.Equal(500, body.Status);
        Assert.False(body.Detail?.Contains("Intentional pipeline", StringComparison.Ordinal) ?? false);
        Assert.False(body.Detail?.Contains("StackTrace", StringComparison.OrdinalIgnoreCase) ?? false);
        Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
    }

    [Fact]
    public async Task UnknownRouteReturnsProblemDetailsWithTraceId()
    {
        using var response = await _client.GetAsync("/api/v1/does-not-exist");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(body);
        Assert.Equal(404, body.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.TraceId));
    }

    [Fact]
    public async Task DiagnosticsEndpointIsNotAvailableOutsideDevelopment()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Cors:AllowedOrigins:0", "https://example.com");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=localhost,1433;Database=ShopCore_Test;User Id=sa;Password=Pass_word123!;TrustServerCertificate=True");
        });
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/diagnostics/error");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record ProblemDetailsResponse(int? Status, string? Detail, string? TraceId);
}
