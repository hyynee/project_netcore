using ShopCore.UnitTests.Support;

namespace ShopCore.UnitTests.Persistence;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = DockerAvailability.SkipReason;
        }
    }
}
