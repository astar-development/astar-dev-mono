namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenObjectExtensions
{
    [Fact]
    public void when_to_json_is_called_then_returns_the_expected_string() =>
        new AnyClass()
            .ToJson()
            .ShouldMatchApproved();
}