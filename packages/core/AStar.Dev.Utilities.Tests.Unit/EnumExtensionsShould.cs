namespace AStar.Dev.Utilities;

public sealed class EnumExtensionsShould
{
    [Fact]
    public void ContainTheParseMethodReturningTheExpectedValue() =>
        "Defined".ParseEnum<AnyEnum>().ShouldBe(AnyEnum.Defined);

    [Fact]
    public void ContainTheParseMethodWhichThrowsArgumentExceptionWhenTheValueIsNotFound()
    {
        Action parseStringAction = () => "ThisDoesntExitst".ParseEnum<AnyEnum>();

        _ = parseStringAction.ShouldThrow<ArgumentException>();
    }
}