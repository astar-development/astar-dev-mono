using AStarDev.FunctionalParadigm.Composition;
namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenPipeAsync
{
    [Fact]
    public async Task when_value_is_piped_through_an_async_function_then_result_is_returned()
    {
        int actual = await 5.PipeAsync(value => Task.FromResult(value * 3));

        actual.ShouldBe(15);
    }
}
