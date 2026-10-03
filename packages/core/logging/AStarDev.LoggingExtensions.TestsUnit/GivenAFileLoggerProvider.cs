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

    public void Dispose()
    {
        provider.Dispose();
        var root = Path.GetDirectoryName(directory)!;
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
