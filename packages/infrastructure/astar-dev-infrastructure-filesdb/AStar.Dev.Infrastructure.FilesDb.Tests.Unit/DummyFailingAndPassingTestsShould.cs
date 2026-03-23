namespace AStar.Dev.Infrastructure.FilesDb.Tests.Unit;

public class DummyFailingAndPassingTestsShould
{
    [Fact]
    public void Pass() => Assert.Equal(4, Add(2, 2));

    [Fact]
    public void PassNow() => Assert.NotEqual(5, Add(2, 2));

    [Theory]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void PassBasedOnNewExpectedValueSupplied(int value, bool expectedValue)
        => Assert.Equal(expectedValue, IsOdd(value));

    private static bool IsOdd(int value) => value % 2 == 1;

    private static int Add(int x, int y) => x + y;
}