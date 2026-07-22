namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenConstants
{
    [Fact]
    public void when_web_deserialisation_settings_are_requested_then_returns_the_expected_settings() =>
        Constants.WebDeserialisationSettings
                 .ToJson()
                 .ShouldMatchApproved();
}
