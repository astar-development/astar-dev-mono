namespace AStar.Dev.Utilities;

public sealed class ObjectExtensionsShould
{
    [Fact]
    public void ContainTheToJsonMethodWhichReturnsTheExpectedString() =>
        new AnyClass()
            .ToJson()
            .ShouldMatchApproved();
}