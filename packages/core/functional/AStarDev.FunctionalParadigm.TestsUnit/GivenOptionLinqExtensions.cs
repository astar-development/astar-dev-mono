namespace AStar.Dev.FunctionalParadigm.TestsUnit;

public sealed class GivenOptionLinqExtensions
{
    [Fact]
    public void when_select_is_called_on_some_then_projects_the_value() =>
        Option.Some(3).Select(value => value * 2).ShouldBe(Option.Some(6));

    [Fact]
    public void when_select_is_called_on_none_then_returns_none() =>
        Option.None<int>().Select(value => value * 2).ShouldBe(Option.None<int>());

    [Fact]
    public void when_selectmany_is_called_on_some_and_bind_returns_some_then_projects_the_combined_value()
    {
        var option = Option.Some(2);

        var actual = option.SelectMany(value => Option.Some(value * 10), (value, intermediate) => value + intermediate);

        actual.ShouldBe(Option.Some(22));
    }

    [Fact]
    public void when_selectmany_is_called_on_some_and_bind_returns_none_then_returns_none()
    {
        var option = Option.Some(2);

        var actual = option.SelectMany(_ => Option.None<int>(), (value, intermediate) => value + intermediate);

        actual.ShouldBe(Option.None<int>());
    }

    [Fact]
    public void when_selectmany_is_called_on_none_then_returns_none_without_invoking_bind()
    {
        var option = Option.None<int>();
        bool bindInvoked = false;

        var actual = option.SelectMany(value =>
        {
            bindInvoked = true;

            return Option.Some(value);
        }, (value, intermediate) => value + intermediate);

        actual.ShouldBe(Option.None<int>());
        bindInvoked.ShouldBeFalse();
    }

    [Fact]
    public void when_used_with_a_single_from_clause_query_expression_then_projects_the_value()
    {
        var option = Option.Some(5);

        var actual = from value in option select value * 3;

        actual.ShouldBe(Option.Some(15));
    }

    [Fact]
    public void when_used_with_two_from_clauses_query_expression_then_projects_the_combined_value()
    {
        var first = Option.Some(4);
        var second = Option.Some(6);

        var actual = from firstValue in first
                     from secondValue in second
                     select firstValue + secondValue;

        actual.ShouldBe(Option.Some(10));
    }

    [Fact]
    public async Task when_selectawaitasync_is_called_on_a_task_of_some_then_projects_the_value()
    {
        var optionTask = Task.FromResult(Option.Some(7));

        var actual = await optionTask.SelectAwaitAsync(value => Task.FromResult(value * 2));

        actual.ShouldBe(Option.Some(14));
    }

    [Fact]
    public async Task when_selectawaitasync_is_called_on_a_task_of_none_then_returns_none_without_invoking_selector()
    {
        var optionTask = Task.FromResult(Option.None<int>());
        bool selectorInvoked = false;

        var actual = await optionTask.SelectAwaitAsync(value =>
        {
            selectorInvoked = true;

            return Task.FromResult(value * 2);
        });

        actual.ShouldBe(Option.None<int>());
        selectorInvoked.ShouldBeFalse();
    }
}
