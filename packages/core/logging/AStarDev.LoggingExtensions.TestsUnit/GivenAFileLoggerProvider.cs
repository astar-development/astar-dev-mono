using AStarDev.LoggingExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace AStarDev.LoggingExtensions.TestsUnit;

public sealed class GivenAFileLoggerProvider : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"astardev-filelogger-{Guid.CreateVersion7():N}", "logs");
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 9, 30, 10, 15, 30, TimeSpan.Zero));
    private readonly FileLoggerProvider provider;

    public GivenAFileLoggerProvider()
    {
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        provider = new(directory, "app", clock);
    }

    [Fact]
    public void when_an_entry_is_logged_then_it_is_appended_to_that_days_file_creating_the_directory()
    {
        provider.CreateLogger("Some.Category").LogInformation("Scrape started");

        File.ReadAllText(provider.FilePathFor(clock.GetLocalNow())).ShouldBe($"2026-09-30 10:15:30.000 +00:00 [Information] Some.Category: Scrape started{Environment.NewLine}");
    }

    [Fact]
    public void when_the_day_changes_then_entries_go_to_a_new_file()
    {
        var logger = provider.CreateLogger("Some.Category");
        logger.LogInformation("first");
        clock.Advance(TimeSpan.FromDays(1));
        logger.LogInformation("second");

        (Path.GetFileName(Directory.GetFiles(directory).Order().First()), Path.GetFileName(Directory.GetFiles(directory).Order().Last())).ShouldBe(("app-20260930.log", "app-20261001.log"));
    }

    [Fact]
    public void when_an_exception_is_logged_then_it_is_written_after_the_message()
    {
        provider.CreateLogger("Some.Category").LogError(new InvalidOperationException("boom"), "It failed");

        File.ReadAllText(provider.FilePathFor(clock.GetLocalNow())).ShouldContain("It failed" + Environment.NewLine + "System.InvalidOperationException: boom");
    }

    [Fact]
    public void when_the_log_level_is_none_then_nothing_is_written()
    {
        provider.CreateLogger("Some.Category").Log(LogLevel.None, "ignored");

        Directory.Exists(directory).ShouldBeFalse();
    }

    [Fact]
    public void when_the_log_file_cannot_be_written_then_the_failure_is_swallowed()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(directory)!);
        File.WriteAllText(directory, "a file where the log directory should be");

        Should.NotThrow(() => provider.CreateLogger("Some.Category").LogInformation("cannot be written"));
    }

    [Fact]
    public void when_added_to_the_logging_builder_then_loggers_write_to_the_file()
    {
        using var serviceProvider = new ServiceCollection().AddLogging(builder => builder.AddFileLogging(directory, "app")).BuildServiceProvider();

        serviceProvider.GetRequiredService<ILogger<GivenAFileLoggerProvider>>().LogInformation("through the builder");

        Directory.GetFiles(directory).Single().ShouldContain("app-");
        File.ReadAllText(Directory.GetFiles(directory).Single()).ShouldContain("through the builder");
    }

    [Fact]
    public void when_added_with_a_filter_then_only_the_entries_it_admits_are_written()
    {
        using var serviceProvider = new ServiceCollection().AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFileLogging(directory, "filtered", (level, _) => level >= LogLevel.Warning)).BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<GivenAFileLoggerProvider>>();

        logger.LogDebug("quiet detail");
        logger.LogError("loud failure");

        var written = File.ReadAllText(Directory.GetFiles(directory, "filtered-*").Single());
        (written.Contains("loud failure", StringComparison.Ordinal), written.Contains("quiet detail", StringComparison.Ordinal)).ShouldBe((true, false));
    }

    public void Dispose()
    {
        provider.Dispose();
        var root = Path.GetDirectoryName(directory)!;
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void when_a_filter_refuses_an_entry_then_nothing_is_written()
    {
        using var refusing = new FileLoggerProvider(directory, "refused", clock, (_, _) => false);

        refusing.CreateLogger("Some.Category").LogInformation("hidden");

        File.Exists(refusing.FilePathFor(clock.GetLocalNow())).ShouldBeFalse();
    }

    [Fact]
    public void when_a_filter_reads_the_scope_then_it_sees_the_values_of_the_scopes_of_the_call()
    {
        var seen = new List<string?>();
        using var reading = new FileLoggerProvider(directory, "scoped", clock, (_, scope) =>
        {
            seen.Add(scope.TryGetValue("AccountIdHash", out var value) ? value?.ToString() : null);

            return true;
        });
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(reading).SetMinimumLevel(LogLevel.Trace));
        var logger = factory.CreateLogger("Some.Category");

        using (logger.BeginScope(new Dictionary<string, object?> { ["AccountIdHash"] = "abc123" }))
            logger.LogDebug("inside");

        logger.LogDebug("outside");

        seen.ShouldBe(["abc123", null]);
    }

    [Fact]
    public void when_a_filter_admits_by_level_then_only_those_entries_are_written()
    {
        using var warningsOnly = new FileLoggerProvider(directory, "levels", clock, (level, _) => level >= LogLevel.Warning);
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(warningsOnly).SetMinimumLevel(LogLevel.Trace));
        var logger = factory.CreateLogger("Some.Category");

        logger.LogDebug("debug");
        logger.LogWarning("warning");

        var written = File.ReadAllText(warningsOnly.FilePathFor(clock.GetLocalNow()));
        (written.Contains("warning", StringComparison.Ordinal), written.Contains("debug", StringComparison.Ordinal)).ShouldBe((true, false));
    }

    [Fact]
    public void when_there_is_no_filter_then_every_entry_is_written()
    {
        provider.CreateLogger("Some.Category").LogDebug("debug detail");

        File.ReadAllText(provider.FilePathFor(clock.GetLocalNow())).ShouldContain("debug detail");
    }
}
