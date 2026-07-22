using AStar.Dev.FunctionalParadigm.Composition;

namespace AStar.Dev.FunctionalParadigm.Tests.Unit;

public class GivenCompose
{
    [Fact]
    public void when_composed_function_is_invoked_then_first_runs_before_second()
    {
        Func<int, int> addOne = value => value + 1;
        Func<int, string> describe = value => $"value-{value}";

        var composed = addOne.Compose(describe);
        string actual = composed(4);

        actual.ShouldBe("value-5");
    }

    [Fact]
    public void when_composed_function_order_changes_then_result_reflects_new_order()
    {
        Func<int, int> doubleIt = value => value * 2;
        Func<int, int> addTen = value => value + 10;

        var doubledThenAddedTen = doubleIt.Compose(addTen);
        var addedTenThenDoubled = addTen.Compose(doubleIt);

        doubledThenAddedTen(3).ShouldBe(16);
        addedTenThenDoubled(3).ShouldBe(26);
    }
}
