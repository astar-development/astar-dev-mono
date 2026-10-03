namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenUnit
{
    [Fact]
    public void when_unit_values_are_compared_then_they_are_all_equal()
    {
        var a = Unit.Instance;
        var b = new Unit();

        a.ShouldBe(b);
        Unit.Instance.ShouldBe(a);
    }
}
