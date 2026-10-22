using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShopCore.Api.Configuration;
using ShopCore.Api.Infrastructure;

namespace ShopCore.UnitTests.Configuration;

public sealed class KnownProxyForwardedHeadersMiddlewareTests
{
    [Fact]
    public async Task ForwardedForFromAnUnknownRemoteIsIgnored()
    {
        var context = CreateContext("198.51.100.7", "203.0.113.5");

        await InvokeAsync(context, trustForwardedHeaders: true, knownProxies: ["10.0.0.1"]);

        Assert.Equal("198.51.100.7", context.Connection.RemoteIpAddress?.ToString());
    }

    [Fact]
    public async Task ForwardedForFromAKnownProxyIsApplied()
    {
        var context = CreateContext("10.0.0.1", "203.0.113.5");

        await InvokeAsync(context, trustForwardedHeaders: true, knownProxies: ["10.0.0.1"]);

        Assert.Equal("203.0.113.5", context.Connection.RemoteIpAddress?.ToString());
    }

    [Fact]
    public async Task TheRightMostAddressThatIsNotAKnownProxyWins()
    {
        var context = CreateContext("10.0.0.1", "192.0.2.66, 203.0.113.5");

        await InvokeAsync(context, trustForwardedHeaders: true, knownProxies: ["10.0.0.1", "203.0.113.5"]);

        Assert.Equal("192.0.2.66", context.Connection.RemoteIpAddress?.ToString());
    }

    [Fact]
    public async Task AForwardedHeaderIsIgnoredWhenTrustIsDisabled()
    {
        var context = CreateContext("10.0.0.1", "203.0.113.5");

        await InvokeAsync(context, trustForwardedHeaders: false, knownProxies: ["10.0.0.1"]);

        Assert.Equal("10.0.0.1", context.Connection.RemoteIpAddress?.ToString());
    }

    [Fact]
    public async Task AKnownNetworkIsAcceptedAsProxy()
    {
        var context = CreateContext("10.1.2.3", "203.0.113.5");

        await InvokeAsync(context, trustForwardedHeaders: true, knownNetworks: ["10.0.0.0/8"]);

        Assert.Equal("203.0.113.5", context.Connection.RemoteIpAddress?.ToString());
    }

    [Fact]
    public async Task ForwardedProtoIsAppliedOnlyForAKnownProxy()
    {
        var trusted = CreateContext("10.0.0.1", forwardedProto: "https");
        var untrusted = CreateContext("198.51.100.7", forwardedProto: "https");

        await InvokeAsync(trusted, trustForwardedHeaders: true, knownProxies: ["10.0.0.1"]);
        await InvokeAsync(untrusted, trustForwardedHeaders: true, knownProxies: ["10.0.0.1"]);

        Assert.Equal("https", trusted.Request.Scheme);
        Assert.Equal("http", untrusted.Request.Scheme);
    }

    private static DefaultHttpContext CreateContext(
        string remoteAddress,
        string? forwardedFor = null,
        string? forwardedProto = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = Uri.UriSchemeHttp;
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        if (forwardedFor is not null)
        {
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }

        if (forwardedProto is not null)
        {
            context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        }

        return context;
    }

    private static Task InvokeAsync(
        HttpContext context,
        bool trustForwardedHeaders,
        string[]? knownProxies = null,
        string[]? knownNetworks = null)
    {
        var options = Options.Create(new ProxyOptions
        {
            TrustForwardedHeaders = trustForwardedHeaders,
            KnownProxies = knownProxies ?? [],
            KnownNetworks = knownNetworks ?? [],
        });
        var middleware = new KnownProxyForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            options,
            NullLogger<KnownProxyForwardedHeadersMiddleware>.Instance);

        return middleware.InvokeAsync(context);
    }
}
