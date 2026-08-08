namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public sealed class GivenAnEnumerable
{
    [Fact]
    public void when_first_or_none_is_called_with_a_matching_predicate_then_some_is_returned()
    {
        var list = new List<string> { "apple", "banana", "cherry" };

        var result = list.FirstOrNone(s => s.StartsWith('b'));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        var some = result as Option<string>.Some;
        some!.Value.ShouldBe("banana");
    }

    [Fact]
    public void when_first_or_none_is_called_with_no_matching_predicate_then_none_is_returned()
    {
        var list = new List<int> { 1, 2, 3 };

        var result = list.FirstOrNone(n => n > 10);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_first_or_none_is_called_on_an_empty_sequence_then_none_is_returned()
    {
        var list = new List<int>();

        var result = list.FirstOrNone(n => n == 0);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void when_first_or_none_is_called_then_the_first_matching_item_is_returned()
    {
        var list = new List<int> { 2, 4, 6 };

        var result = list.FirstOrNone(n => n % 2 == 0);

        var some = result.ShouldBeOfType<Option<int>.Some>();
        some.Value.ShouldBe(2);
    }
}
