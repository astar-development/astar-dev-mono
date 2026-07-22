namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenLinqExtensions
{
    [Fact]
    public void when_first_or_none_is_called_on_an_empty_sequence_then_none_is_returned()
    {
        var result = new List<string>().FirstOrNone();

        result.ShouldBe(Option.None<string>());
    }

    [Fact]
    public void when_first_or_none_is_called_on_a_non_empty_sequence_then_the_first_item_is_returned_as_some()
    {
        var result = new List<string> { "a", "b", "c" }.FirstOrNone();

        result.ShouldBe(new Option<string>.Some("a"));
    }

    [Fact]
    public void when_first_or_none_with_a_predicate_finds_a_match_then_the_matching_item_is_returned_as_some()
    {
        var result = new List<string> { "a", "b", "c" }.FirstOrNone(item => item == "b");

        result.ShouldBe(new Option<string>.Some("b"));
    }

    [Fact]
    public void when_first_or_none_with_a_predicate_finds_no_match_then_none_is_returned()
    {
        var result = new List<string> { "a", "b", "c" }.FirstOrNone(item => item == "z");

        result.ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_first_or_none_async_is_called_on_an_empty_async_sequence_then_none_is_returned()
    {
        var result = await EmptyAsyncSequence().FirstOrNoneAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(Option.None<string>());
    }

    [Fact]
    public async Task when_first_or_none_async_is_called_on_a_non_empty_async_sequence_then_the_first_item_is_returned_as_some()
    {
        var result = await AsyncSequence("a", "b", "c").FirstOrNoneAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(new Option<string>.Some("a"));
    }

    [Fact]
    public async Task when_first_or_none_async_with_a_predicate_finds_a_match_then_the_matching_item_is_returned_as_some()
    {
        var result = await AsyncSequence("a", "b", "c").FirstOrNoneAsync(item => item == "b", TestContext.Current.CancellationToken);

        result.ShouldBe(new Option<string>.Some("b"));
    }

    [Fact]
    public async Task when_first_or_none_async_with_a_predicate_finds_no_match_then_none_is_returned()
    {
        var result = await AsyncSequence("a", "b", "c").FirstOrNoneAsync(item => item == "z", TestContext.Current.CancellationToken);

        result.ShouldBe(Option.None<string>());
    }

    private static async IAsyncEnumerable<string> EmptyAsyncSequence()
    {
        await Task.CompletedTask;

        yield break;
    }

    private static async IAsyncEnumerable<string> AsyncSequence(params string[] items)
    {
        foreach (string item in items)
        {
            await Task.CompletedTask;

            yield return item;
        }
    }
}
