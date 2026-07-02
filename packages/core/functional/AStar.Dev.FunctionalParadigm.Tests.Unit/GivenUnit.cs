using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenUnit
{
    [Fact]
    public void unit_value_is_singleton_like()
    {
        var a = global::AStar.Dev.FunctionalParadigm.Unit.Value;
        var b = new global::AStar.Dev.FunctionalParadigm.Unit();

        a.ShouldBe(b);
        global::AStar.Dev.FunctionalParadigm.Unit.Value.ShouldBe(a);
    }
}
