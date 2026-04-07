using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using AStar.Dev.OneDriveSync.Features.LogViewer;
using Serilog.Events;
using Serilog.Parsing;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Features.LogViewer;

public sealed class GivenAnInMemoryLogSink
{
    [Fact]
    public void when_fewer_than_capacity_entries_emitted_then_all_are_retained()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Message 1"));
        sut.Emit(BuildLogEvent(LogEventLevel.Warning, "Message 2"));

        sut.GetSnapshot().Count.ShouldBe(2);
    }

    [Fact]
    public void when_entries_exceed_capacity_then_oldest_is_evicted()
    {
        const int capacity = 3;
        var sut = new InMemoryLogSink(capacity);

        sut.Emit(BuildLogEvent(LogEventLevel.Information, "First"));
        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Second"));
        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Third"));
        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Fourth"));

        var snapshot = sut.GetSnapshot();

        snapshot.Count.ShouldBe(capacity);
        snapshot.ShouldNotContain(entry => entry.RenderedMessage == "First");
        snapshot.ShouldContain(entry => entry.RenderedMessage == "Fourth");
    }

    [Fact]
    public void when_entry_emitted_then_observable_fires()
    {
        var sut = new InMemoryLogSink();
        var received = new List<LogEntry>();

        using var _ = sut.EntryAdded.Subscribe(received.Add);

        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Test message"));

        received.Count.ShouldBe(1);
    }

    [Fact]
    public void when_entry_emitted_then_log_level_is_preserved()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(BuildLogEvent(LogEventLevel.Warning, "A warning"));

        sut.GetSnapshot()[0].Level.ShouldBe(LogEventLevel.Warning);
    }

    [Fact]
    public void when_entry_emitted_then_timestamp_is_preserved()
    {
        var sut = new InMemoryLogSink();
        var now = DateTimeOffset.UtcNow;

        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Timed", timestamp: now));

        sut.GetSnapshot()[0].Timestamp.ShouldBe(now);
    }

    [Fact]
    public void when_message_contains_email_then_it_is_redacted()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Syncing account user@example.com completed"));

        sut.GetSnapshot()[0].RenderedMessage.ShouldNotContain("user@example.com");
        sut.GetSnapshot()[0].RenderedMessage.ShouldContain("[email redacted]");
    }

    [Fact]
    public void when_log_event_has_account_id_property_then_entry_captures_it()
    {
        var sut = new InMemoryLogSink();
        var accountId = "3057f494-687d-4abb-a653-4b8066230b6e";

        sut.Emit(BuildLogEventWithAccountId(LogEventLevel.Information, "Sync started", accountId));

        sut.GetSnapshot()[0].AccountId.ShouldBe(accountId);
    }

    [Fact]
    public void when_log_event_has_no_account_id_property_then_account_id_is_null()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(BuildLogEvent(LogEventLevel.Information, "App started"));

        sut.GetSnapshot()[0].AccountId.ShouldBeNull();
    }

    [Fact]
    public void when_snapshot_called_then_returns_copy_not_live_collection()
    {
        var sut = new InMemoryLogSink();
        sut.Emit(BuildLogEvent(LogEventLevel.Information, "First"));

        var snapshot = sut.GetSnapshot();
        sut.Emit(BuildLogEvent(LogEventLevel.Information, "Second"));

        snapshot.Count.ShouldBe(1);
    }

    [Fact]
    public void when_capacity_is_zero_then_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InMemoryLogSink(0));
    }

    [Fact]
    public void when_capacity_is_negative_then_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InMemoryLogSink(-1));
    }

    [Fact]
    public void when_emit_is_called_with_null_then_throws()
    {
        var sut = new InMemoryLogSink();

        Should.Throw<ArgumentNullException>(() => sut.Emit(null!));
    }

    [Fact]
    public void when_message_contains_multiple_emails_then_all_are_redacted()
    {
        var sut = new InMemoryLogSink();

        sut.Emit(BuildLogEvent(LogEventLevel.Warning, "Conflict between user@a.com and admin@b.com"));

        var message = sut.GetSnapshot()[0].RenderedMessage;
        message.ShouldNotContain("user@a.com");
        message.ShouldNotContain("admin@b.com");
        message.ShouldContain("[email redacted]");
    }

    [Fact]
    public async Task when_disposed_then_entry_added_observable_completes()
    {
        var sut = new InMemoryLogSink();
        bool completed = false;

        using var _ = sut.EntryAdded.Subscribe(onNext: static _ => { }, onCompleted: () => completed = true);

        sut.Dispose();
        await Task.Yield();

        completed.ShouldBeTrue();
    }

    private static LogEvent BuildLogEvent(LogEventLevel level, string messageText, DateTimeOffset? timestamp = null)
    {
        var template = new MessageTemplateParser().Parse(messageText);

        return new LogEvent(
            timestamp ?? DateTimeOffset.UtcNow,
            level,
            exception: null,
            template,
            properties: []);
    }

    private static LogEvent BuildLogEventWithAccountId(LogEventLevel level, string messageText, string accountId)
    {
        var template   = new MessageTemplateParser().Parse(messageText);
        var properties = new[] { new LogEventProperty("AccountId", new ScalarValue(accountId)) };

        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            exception: null,
            template,
            properties);
    }
}
