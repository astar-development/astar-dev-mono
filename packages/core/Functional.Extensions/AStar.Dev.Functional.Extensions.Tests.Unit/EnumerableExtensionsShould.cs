namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class EnumerableExtensionsShould
{
    [Fact]
    public void FirstOrNone_ShouldReturnSome_WhenPredicateMatches()
    {
        var list = new List<string> { "apple", "banana", "cherry" };

        var result = list.FirstOrNone(s => s.StartsWith('b'));

        result.ShouldBeOfType<Option<string>.Some>();
        var some = result as Option<string>.Some;
        some!.Value.ShouldBe("banana");
    }

    [Fact]
    public void FirstOrNone_ShouldReturnNone_WhenNoPredicateMatches()
    {
        var list = new List<int> { 1, 2, 3 };

        var result = list.FirstOrNone(n => n > 10);

        result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void FirstOrNone_ShouldReturnNone_ForEmptySequence()
    {
        var list = new List<int>();

        var result = list.FirstOrNone(n => n == 0);

        result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void FirstOrNone_ShouldReturnFirstMatchingItem()
    {
        var list = new List<int> { 2, 4, 6 };

        var result = list.FirstOrNone(n => n % 2 == 0);

        var some = result.ShouldBeOfType<Option<int>.Some>();
        some.Value.ShouldBe(2);
    }
}
