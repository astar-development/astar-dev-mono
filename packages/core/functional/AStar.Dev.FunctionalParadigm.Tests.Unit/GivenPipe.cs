using AStar.Dev.FunctionalParadigm.Composition;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenPipe
{
    [Fact]
    public void when_value_is_piped_then_function_result_is_returned() =>
        5.Pipe(value => value * 2).ShouldBe(10);

    [Fact]
    public void when_value_is_piped_through_a_different_type_then_the_transformed_value_is_returned() =>
        5.Pipe(value => $"{value}").ShouldBe("5");
}
