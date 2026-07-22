namespace AStar.Dev.Utilities.Tests.Unit;

public sealed class GivenEnumExtensions
{
    [Fact]
    public void when_parse_is_called_with_a_valid_value_then_returns_the_expected_value() =>
        "Defined".ParseEnum<AnyEnum>().ShouldBe(AnyEnum.Defined);

    [Fact]
    public void when_parse_is_called_with_a_different_casing_then_returns_the_expected_value() =>
        "defined".ParseEnum<AnyEnum>().ShouldBe(AnyEnum.Defined);

    [Fact]
    public void when_parse_is_called_with_an_unknown_value_then_throws_argument_exception()
    {
        Action parseStringAction = () => "ThisDoesntExitst".ParseEnum<AnyEnum>();

        _ = parseStringAction.ShouldThrow<ArgumentException>();
    }
}