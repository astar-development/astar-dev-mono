using AStar.Dev.FunctionalParadigm.Composition;
using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenFunctionTap
{
    [Fact]
    public void when_value_is_tapped_then_side_effect_runs_and_original_value_is_returned()
    {
        var sideEffectValue = 0;

        var actual = 7.Tap(value => sideEffectValue = value);

        actual.ShouldBe(7);
        sideEffectValue.ShouldBe(7);
    }
}
