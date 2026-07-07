using AStar.Dev.FunctionalParadigm.Composition;
using Shouldly;
using Xunit;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenPipeAsync
{
    [Fact]
    public async Task when_value_is_piped_through_an_async_function_then_result_is_returned()
    {
        var actual = await 5.PipeAsync(value => Task.FromResult(value * 3));

        actual.ShouldBe(15);
    }
}
