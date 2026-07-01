using AStar.Dev.OneDrive.Sync.Client.LogViewer;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.LogViewer;

public sealed class GivenAnInMemoryLogSink
{
    private static LogEvent MakeEvent(LogEventLevel level = LogEventLevel.Information, string message = "test message", params LogEventProperty[] properties)
        => new(DateTimeOffset.UtcNow, level, null, new Serilog.Events.MessageTemplate(message, []), properties);

    private static LogEventProperty AccountIdProperty(string accountId)
        => new("AccountId", new ScalarValue(accountId));

    [Fact]
    public void when_emit_called_then_entry_appears_in_snapshot()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(MakeEvent());

        sut.GetSnapshot().ShouldHaveSingleItem();
    }

    [Fact]
    public void when_emit_called_multiple_times_then_snapshot_count_matches()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(MakeEvent());
        sut.Emit(MakeEvent());
        sut.Emit(MakeEvent());

        sut.GetSnapshot().Count.ShouldBe(3);
    }

    [Fact]
    public void when_emit_called_then_entry_level_matches_event_level()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(MakeEvent(LogEventLevel.Warning));

        sut.GetSnapshot()[0].Level.ShouldBe(LogEventLevel.Warning);
    }

    [Fact]
    public void when_emit_called_with_account_id_property_then_entry_account_id_is_extracted()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(MakeEvent(properties: AccountIdProperty("acc-test")));

        sut.GetSnapshot()[0].AccountId.ShouldBe("acc-test");
    }

    [Fact]
    public void when_emit_called_without_account_id_property_then_entry_account_id_is_null()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(MakeEvent());

        sut.GetSnapshot()[0].AccountId.ShouldBeNull();
    }

    [Fact]
    public void when_emit_called_then_entry_added_observable_fires()
    {
        var sut = new InMemoryLogSink();
        LogEntry? received = null;
        sut.EntryAdded.Subscribe(e => received = e);

        sut.Emit(MakeEvent());

        received.ShouldNotBeNull();
    }

    [Fact]
    public void when_emit_called_then_entry_added_observable_fires_correct_level()
    {
        var sut = new InMemoryLogSink();
        LogEntry? received = null;
        sut.EntryAdded.Subscribe(e => received = e);

        sut.Emit(MakeEvent(LogEventLevel.Error));

        received!.Level.ShouldBe(LogEventLevel.Error);
    }

    [Fact]
    public void when_dispose_called_then_observable_is_completed()
    {
        var sut = new InMemoryLogSink();
        bool completed = false;
        sut.EntryAdded.Subscribe(_ => { }, () => completed = true);

        sut.Dispose();

        completed.ShouldBeTrue();
    }

    [Fact]
    public void when_get_snapshot_called_then_returns_immutable_copy()
    {
        var sut = new InMemoryLogSink();
        sut.Emit(MakeEvent());
        var snapshot = sut.GetSnapshot();

        sut.Emit(MakeEvent());

        snapshot.Count.ShouldBe(1);
        sut.GetSnapshot().Count.ShouldBe(2);
    }

    [Fact]
    public void when_default_capacity_is_inspected_then_value_is_500()
    {
        InMemoryLogSink.DefaultCapacity.ShouldBe(500);
    }
}
