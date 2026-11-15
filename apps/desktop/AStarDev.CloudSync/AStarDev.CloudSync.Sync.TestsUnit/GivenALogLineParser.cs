using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenALogLineParser
{
    private const string Plain = "2026-11-15 10:20:30.123 +00:00 [Information] AStarDev.CloudSync.Sync.Runner: Pass finished";

    [Fact]
    public void when_a_line_has_no_account_then_its_parts_are_read_and_the_account_is_none()
    {
        var line = LogLineParser.Parse(Plain).Match(parsed => parsed, () => throw new InvalidOperationException("The line must parse."));

        (line.Time, line.Level, line.AccountHash.Match(_ => false, () => true), line.Category, line.Message).ShouldBe((new DateTimeOffset(2026, 11, 15, 10, 20, 30, 123, TimeSpan.Zero), LogLevel.Information, true, "AStarDev.CloudSync.Sync.Runner", "Pass finished"));
    }

    [Fact]
    public void when_a_line_names_an_account_then_the_hash_is_read()
    {
        var line = LogLineParser.Parse("2026-11-15 10:20:30.123 +00:00 [Debug] [AccountIdHash=ab12cd] Some.Category: Listed 5 items").Match(parsed => parsed, () => throw new InvalidOperationException("The line must parse."));

        (line.AccountHash.Match(hash => hash, () => string.Empty), line.Level, line.Message).ShouldBe(("ab12cd", LogLevel.Debug, "Listed 5 items"));
    }

    [Fact]
    public void when_the_message_holds_a_colon_then_the_category_is_the_text_before_the_first_one()
    {
        var line = LogLineParser.Parse("2026-11-15 10:20:30.123 +00:00 [Warning] Some.Category: Retry in 5s: throttled").Match(parsed => parsed, () => throw new InvalidOperationException("The line must parse."));

        (line.Category, line.Message).ShouldBe(("Some.Category", "Retry in 5s: throttled"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   at Some.Method() in file.cs:line 10")]
    [InlineData("System.InvalidOperationException: boom")]
    [InlineData("2026-11-15 10:20:30.123 +00:00 [Nonsense] Some.Category: text")]
    [InlineData("2026-13-45 10:20:30.123 +00:00 [Information] Some.Category: text")]
    public void when_a_line_does_not_start_an_entry_then_none_is_returned(string text) => LogLineParser.Parse(text).Match(_ => false, () => true).ShouldBeTrue();
}
