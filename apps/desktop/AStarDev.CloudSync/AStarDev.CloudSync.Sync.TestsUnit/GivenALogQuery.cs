using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALogQuery
{
    private static readonly DateTimeOffset Noon = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static LogLine Line(LogLevel level, string account, string message) => LogLineFactory.Create(Noon, level, account.Length == 0 ? Option.None<string>() : Option.Some(account), "Cat", message);

    [Fact]
    public void when_the_query_asks_for_everything_then_every_line_matches()
    {
        var query = LogQueryFactory.CreateEverything();

        query.Matches(Line(LogLevel.Trace, "", "x")).ShouldBeTrue();
    }

    [Fact]
    public void when_an_account_is_chosen_then_only_that_accounts_lines_match()
    {
        var query = LogQueryFactory.Create(Option.Some("abc"), LogLevel.Trace, "");

        (query.Matches(Line(LogLevel.Debug, "abc", "x")), query.Matches(Line(LogLevel.Debug, "xyz", "x")), query.Matches(Line(LogLevel.Debug, "", "x"))).ShouldBe((true, false, false));
    }

    [Fact]
    public void when_a_minimum_level_is_chosen_then_lower_lines_do_not_match()
    {
        var query = LogQueryFactory.Create(Option.None<string>(), LogLevel.Warning, "");

        (query.Matches(Line(LogLevel.Information, "", "x")), query.Matches(Line(LogLevel.Warning, "", "x")), query.Matches(Line(LogLevel.Error, "", "x"))).ShouldBe((false, true, true));
    }

    [Fact]
    public void when_text_is_given_then_it_is_found_in_the_message_or_category_ignoring_case()
    {
        var query = LogQueryFactory.Create(Option.None<string>(), LogLevel.Trace, "THROTTLE");

        (query.Matches(Line(LogLevel.Debug, "", "was throttled")), query.Matches(LogLineFactory.Create(Noon, LogLevel.Debug, Option.None<string>(), "Throttle.Cat", "x")), query.Matches(Line(LogLevel.Debug, "", "fine"))).ShouldBe((true, true, false));
    }

    [Fact]
    public void when_the_text_is_missing_then_it_is_treated_as_empty()
    {
        LogQueryFactory.Create(Option.None<string>(), LogLevel.Trace, null).Text.ShouldBe(string.Empty);
    }
}
