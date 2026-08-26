using AStarDev.LoggingOTel.LogViewer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AStarDev.LoggingOTel.TestsUnit.LogViewer;

public sealed class GivenAnInMemoryLogProcessor
{
    private static ILogger<GivenAnInMemoryLogProcessor> CreateLogger(InMemoryLogProcessor inMemoryLogProcessor)
    {
        var services = new ServiceCollection();
        _ = services.AddLogging(builder => builder.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.AddProcessor(inMemoryLogProcessor);
        }));
        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<ILogger<GivenAnInMemoryLogProcessor>>();
    }

    [Fact]
    public void when_a_log_entry_is_emitted_then_it_appears_in_the_snapshot()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);

        logger.LogInformation("test message");

        sut.GetSnapshot().ShouldHaveSingleItem();
    }

    [Fact]
    public void when_multiple_log_entries_are_emitted_then_snapshot_count_matches()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);

        logger.LogInformation("first");
        logger.LogInformation("second");
        logger.LogInformation("third");

        sut.GetSnapshot().Count.ShouldBe(3);
    }

    [Fact]
    public void when_a_log_entry_is_emitted_then_entry_level_matches_event_level()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);

        logger.LogWarning("test message");

        sut.GetSnapshot()[0].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void when_a_log_entry_is_emitted_then_rendered_message_is_captured()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);

        logger.LogInformation("the expected message was logged to the in memory processor");

        sut.GetSnapshot().ShouldContain(entry => entry.RenderedMessage.Contains("the expected message was logged to the in memory processor"));
    }

    [Fact]
    public void when_a_log_entry_is_emitted_with_an_account_id_property_then_entry_account_id_is_extracted()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);

        logger.LogInformation("account event for {AccountId}", "acc-test");

        sut.GetSnapshot()[0].AccountId.ShouldBe("acc-test");
    }

    [Fact]
    public void when_a_log_entry_is_emitted_without_an_account_id_property_then_entry_account_id_is_null()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);

        logger.LogInformation("no account here");

        sut.GetSnapshot()[0].AccountId.ShouldBeNull();
    }

    [Fact]
    public void when_a_log_entry_is_emitted_then_entry_added_observable_fires()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);
        LogEntry? received = null;
        sut.EntryAdded.Subscribe(e => received = e);

        logger.LogInformation("test message");

        received.ShouldNotBeNull();
    }

    [Fact]
    public void when_a_log_entry_is_emitted_then_entry_added_observable_fires_correct_level()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);
        LogEntry? received = null;
        sut.EntryAdded.Subscribe(e => received = e);

        logger.LogError("test message");

        received!.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public void when_dispose_is_called_then_observable_is_completed()
    {
        var sut = new InMemoryLogProcessor();
        bool completed = false;
        sut.EntryAdded.Subscribe(_ => { }, () => completed = true);

        sut.Dispose();

        completed.ShouldBeTrue();
    }

    [Fact]
    public void when_get_snapshot_is_called_then_it_returns_an_immutable_copy()
    {
        using var sut = new InMemoryLogProcessor();
        var logger = CreateLogger(sut);
        logger.LogInformation("first");
        var snapshot = sut.GetSnapshot();

        logger.LogInformation("second");

        snapshot.Count.ShouldBe(1);
        sut.GetSnapshot().Count.ShouldBe(2);
    }

    [Fact]
    public void when_default_capacity_is_inspected_then_value_is_500()
    {
        InMemoryLogProcessor.DefaultCapacity.ShouldBe(500);
    }
}
