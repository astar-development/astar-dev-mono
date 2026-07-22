namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenUnit
{
    [Fact]
    public void when_unit_values_are_compared_then_they_are_all_equal()
    {
        var a = FunctionalParadigm.Unit.Instance;
        var b = new FunctionalParadigm.Unit();

        a.ShouldBe(b);
        FunctionalParadigm.Unit.Instance.ShouldBe(a);
    }
}
