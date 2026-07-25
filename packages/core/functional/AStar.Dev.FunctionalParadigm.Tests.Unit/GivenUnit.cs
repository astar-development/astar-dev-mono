namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public sealed class GivenUnit
{
    [Fact]
    public void when_unit_values_are_compared_then_they_are_all_equal()
    {
        var a = FunctionalParadigm.UnitFp.Instance;
        var b = new FunctionalParadigm.UnitFp();

        a.ShouldBe(b);
        FunctionalParadigm.UnitFp.Instance.ShouldBe(a);
    }
}
