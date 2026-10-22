namespace ShopCore.Api.Configuration;

public sealed class ProxyOptions
{
    public const string SectionName = "Proxy";

    /// <summary>
    /// Enables the forwarded header middleware. It must stay disabled until the real
    /// reverse proxy addresses are listed, otherwise a client can spoof its own IP.
    /// </summary>
    public bool TrustForwardedHeaders { get; set; }

    public string[] KnownProxies { get; set; } = [];

    public string[] KnownNetworks { get; set; } = [];

    /// <summary>
    /// Reads a list that can be written as a JSON array, as indexed keys
    /// (<c>Proxy:KnownProxies:0</c>) or as one comma separated value
    /// (<c>Proxy:KnownProxies=10.0.0.1,10.0.0.2</c>). The plain environment variable
    /// form does not bind to a list on its own, so it is parsed here.
    /// </summary>
    public static string[] ReadKnownEntries(IConfiguration configuration, string key)
    {
        var section = configuration.GetSection($"{SectionName}:{key}");
        var entries = section
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        if (entries.Length > 0)
        {
            return entries;
        }

        var raw = section.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
