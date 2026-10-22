using Microsoft.Extensions.Configuration;
using ShopCore.Api.Configuration;

namespace ShopCore.UnitTests.Configuration;

public sealed class ProxyOptionsTests
{
    [Fact]
    public void IndexedKeysAreReadAsKnownProxies()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["Proxy:KnownProxies:0"] = "10.0.0.1",
            ["Proxy:KnownProxies:1"] = "10.0.0.2",
        });

        var entries = ProxyOptions.ReadKnownEntries(configuration, nameof(ProxyOptions.KnownProxies));

        Assert.Equal(["10.0.0.1", "10.0.0.2"], entries);
    }

    [Fact]
    public void CommaSeparatedValueIsReadAsKnownNetworks()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["Proxy:KnownNetworks"] = "10.0.0.0/8, 192.168.0.0/16",
        });

        var entries = ProxyOptions.ReadKnownEntries(configuration, nameof(ProxyOptions.KnownNetworks));

        Assert.Equal(["10.0.0.0/8", "192.168.0.0/16"], entries);
    }

    [Fact]
    public void MissingSectionReturnsNoEntries()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["Proxy:TrustForwardedHeaders"] = "true",
        });

        Assert.Empty(ProxyOptions.ReadKnownEntries(configuration, nameof(ProxyOptions.KnownProxies)));
        Assert.Empty(ProxyOptions.ReadKnownEntries(configuration, nameof(ProxyOptions.KnownNetworks)));
    }

    [Fact]
    public void BlankValueReturnsNoEntries()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["Proxy:KnownProxies"] = " , ",
        });

        Assert.Empty(ProxyOptions.ReadKnownEntries(configuration, nameof(ProxyOptions.KnownProxies)));
    }

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
