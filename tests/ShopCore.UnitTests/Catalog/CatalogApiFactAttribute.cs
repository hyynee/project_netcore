using ShopCore.UnitTests.Support;

namespace ShopCore.UnitTests.Catalog;

public sealed class CatalogApiFactAttribute : FactAttribute
{
    public CatalogApiFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = DockerAvailability.SkipReason;
        }
    }
}
