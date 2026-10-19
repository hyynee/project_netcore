using ShopCore.Api.Infrastructure;

namespace ShopCore.UnitTests;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void SqlUniqueConstraintNumbersAreMappedToConflict(int sqlErrorNumber)
    {
        Assert.True(GlobalExceptionHandler.IsUniqueConstraintNumber(sqlErrorNumber));
    }

    [Theory]
    [InlineData(547)]
    [InlineData(1205)]
    [InlineData(50000)]
    public void UnrelatedSqlErrorsAreNotMappedToConflict(int sqlErrorNumber)
    {
        Assert.False(GlobalExceptionHandler.IsUniqueConstraintNumber(sqlErrorNumber));
    }
}
