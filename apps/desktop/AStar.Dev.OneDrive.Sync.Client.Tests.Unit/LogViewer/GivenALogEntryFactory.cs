using AStar.Dev.OneDrive.Sync.Client.LogViewer;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.LogViewer;

public sealed class GivenALogEntryFactory
{
    [Fact]
    public void when_creating_entry_then_timestamp_is_preserved()
    {
        var timestamp = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

        var entry = LogEntryFactory.Create(timestamp, LogEventLevel.Information, "msg", null);

        entry.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void when_creating_entry_then_level_is_preserved()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Error, "msg", null);

        entry.Level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void when_creating_entry_then_rendered_message_is_preserved()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "hello world", null);

        entry.RenderedMessage.ShouldBe("hello world");
    }

    [Fact]
    public void when_creating_entry_with_account_id_then_account_id_is_preserved()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "msg", "acc-123");

        entry.AccountId.ShouldBe("acc-123");
    }

    [Fact]
    public void when_creating_entry_with_null_account_id_then_account_id_is_null()
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, LogEventLevel.Information, "msg", null);

        entry.AccountId.ShouldBeNull();
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose)]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void when_creating_entry_with_each_level_then_level_is_preserved(LogEventLevel level)
    {
        var entry = LogEntryFactory.Create(DateTimeOffset.UtcNow, level, "msg", null);

        entry.Level.ShouldBe(level);
    }
}
