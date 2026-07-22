using AStar.Dev.FunctionalParadigm.Composition;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenFunctionTap
{
    [Fact]
    public void when_value_is_tapped_then_side_effect_runs_and_original_value_is_returned()
    {
        int sideEffectValue = 0;

        int actual = 7.Tap(value => sideEffectValue = value);

        actual.ShouldBe(7);
        sideEffectValue.ShouldBe(7);
    }
}
