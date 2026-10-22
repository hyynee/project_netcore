using System.Net;
using Microsoft.Extensions.Options;
using ShopCore.Api.Configuration;
using ProxyNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace ShopCore.Api.Infrastructure;

public sealed partial class KnownProxyForwardedHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<KnownProxyForwardedHeadersMiddleware> _logger;
    private readonly bool _trustForwardedHeaders;
    private readonly IPAddress[] _knownProxies;
    private readonly ProxyNetwork[] _knownNetworks;

    public KnownProxyForwardedHeadersMiddleware(
        RequestDelegate next,
        IOptions<ProxyOptions> options,
        ILogger<KnownProxyForwardedHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        var proxyOptions = options.Value;
        _trustForwardedHeaders = proxyOptions.TrustForwardedHeaders;
        _knownProxies = proxyOptions.KnownProxies
            .Select(value => IPAddress.TryParse(value, out var address) ? address : null)
            .Where(address => address is not null)
            .Select(address => address!)
            .ToArray();
        _knownNetworks = proxyOptions.KnownNetworks
            .Select(value => ProxyNetwork.TryParse(value, out var network) ? network : null)
            .Where(network => network is not null)
            .Select(network => network!)
            .ToArray();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteAddress = context.Connection.RemoteIpAddress;
        if (_trustForwardedHeaders && IsKnownProxy(remoteAddress))
        {
            ApplyForwardedFor(context);
            ApplyForwardedProto(context);
        }
        else if (HasForwardedHeader(context))
        {
            LogIgnoredForwardedHeaders(_logger, remoteAddress);
        }

        await _next(context);
    }

    private void ApplyForwardedFor(HttpContext context)
    {
        var header = context.Request.Headers["X-Forwarded-For"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return;
        }

        var candidates = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = candidates.Length - 1; index >= 0; index--)
        {
            if (!IPAddress.TryParse(candidates[index], out var address))
            {
                continue;
            }

            if (IsKnownProxy(address))
            {
                continue;
            }

            context.Connection.RemoteIpAddress = address;
            return;
        }
    }

    private static void ApplyForwardedProto(HttpContext context)
    {
        var header = context.Request.Headers["X-Forwarded-Proto"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return;
        }

        var proto = header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (string.Equals(proto, "https", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = Uri.UriSchemeHttps;
        }
    }

    private bool IsKnownProxy(IPAddress? address)
    {
        if (address is null)
        {
            return false;
        }

        if (_knownProxies.Any(proxy => proxy.Equals(address)))
        {
            return true;
        }

        return _knownNetworks.Any(network => network.Contains(address));
    }

    private static bool HasForwardedHeader(HttpContext context) =>
        context.Request.Headers.ContainsKey("X-Forwarded-For") ||
        context.Request.Headers.ContainsKey("X-Forwarded-Proto");

    [LoggerMessage(
        EventId = 4200,
        Level = LogLevel.Debug,
        Message = "Ignored forwarded headers because {RemoteAddress} is not a known proxy.")]
    private static partial void LogIgnoredForwardedHeaders(ILogger logger, IPAddress? remoteAddress);
}
