using AStarDev.LoggingOTel.LogViewer;
using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingOTel.TestsUnit.LogViewer;

public sealed class GivenALogEntryFactory
{
    [Fact]
    public void when_creating_entry_then_timestamp_is_preserved()
    {
        var timestamp = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var entry = LogEntryFactory.Create(timestamp, LogLevel.Information, "msg", null);

        entry.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void when_creating_entry_then_level_is_preserved()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogLevel.Error, "msg", null);

        entry.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public void when_creating_entry_then_rendered_message_is_preserved()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogLevel.Information, "hello world", null);

        entry.RenderedMessage.ShouldBe("hello world");
    }

    [Fact]
    public void when_creating_entry_with_account_id_then_account_id_is_preserved()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogLevel.Information, "msg", "acc-123");

        entry.AccountId.ShouldBe("acc-123");
    }

    [Fact]
    public void when_creating_entry_with_null_account_id_then_account_id_is_null()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogLevel.Information, "msg", null);

        entry.AccountId.ShouldBeNull();
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void when_creating_entry_with_each_level_then_level_is_preserved(LogLevel level)
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, level, "msg", null);

        entry.Level.ShouldBe(level);
    }
}
