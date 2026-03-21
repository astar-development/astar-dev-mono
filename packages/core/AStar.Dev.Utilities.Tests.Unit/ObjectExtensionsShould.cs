namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class ObjectExtensionsShould
{
    [Fact]
    public void ContainTheToJsonMethodWhichReturnsTheExpectedString() =>
        new AnyClass()
            .ToJson()
            .ShouldMatchApproved();
}