using System.IO.Abstractions;
using AStarDev.CloudSync.Logging;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.Logging;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenALogEntryReader
{
    private const string Folder = "/logs";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private LogEntryReader Reader() => new(new FixedLogFileLocator(fileSystem.DirectoryInfo.New(Folder)));

    private void File(string name, params string[] lines)
    {
        _ = fileSystem.Directory.CreateDirectory(Folder);
        fileSystem.File.WriteAllLines($"{Folder}/{name}", lines);
    }

    private async Task<List<LogLine>> ReadAsync(LogQuery query)
    {
        var lines = new List<LogLine>();
        await foreach (var line in Reader().ReadAsync(query, CancellationToken))
            lines.Add(line);

        return lines;
    }

    [Fact]
    public async Task when_there_are_two_days_of_files_then_the_entries_come_oldest_first_across_them()
    {
        File("cloudsync-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: today");
        File("cloudsync-20261114.log", "2026-11-14 09:00:00.000 +00:00 [Information] Cat: yesterday");

        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.Select(line => line.Message).ShouldBe(["yesterday", "today"]);
    }

    [Fact]
    public async Task when_a_day_was_split_by_size_then_the_numbered_files_follow_the_first_in_order()
    {
        File("cloudsync-20261115-10.log", "2026-11-15 09:00:03.000 +00:00 [Information] Cat: fourth");
        File("cloudsync-20261115-2.log", "2026-11-15 09:00:01.000 +00:00 [Information] Cat: second");
        File("cloudsync-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: first");
        File("cloudsync-20261115-3.log", "2026-11-15 09:00:02.000 +00:00 [Information] Cat: third");
        File("cloudsync-20261116.log", "2026-11-16 09:00:00.000 +00:00 [Information] Cat: next day");

        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.Select(line => line.Message).ShouldBe(["first", "second", "third", "fourth", "next day"]);
    }

    [Fact]
    public async Task when_the_folder_does_not_exist_then_nothing_is_returned()
    {
        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_other_files_are_in_the_folder_then_they_are_ignored()
    {
        File("cloudsync-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: mine");
        File("other-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: not mine");
        File("cloudsync-notes.txt", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: not a log");

        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.Select(line => line.Message).ShouldBe(["mine"]);
    }

    [Fact]
    public async Task when_an_exception_follows_an_entry_then_its_lines_join_that_entry()
    {
        File("cloudsync-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Error] Cat: failed", "System.InvalidOperationException: boom", "   at Some.Method()", "2026-11-15 09:00:01.000 +00:00 [Information] Cat: next");

        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.Select(line => line.Message).ShouldBe(["failed\nSystem.InvalidOperationException: boom\n   at Some.Method()", "next"]);
    }

    [Fact]
    public async Task when_a_file_starts_with_text_that_is_not_an_entry_then_that_text_is_dropped()
    {
        File("cloudsync-20261115.log", "   at Orphan.Method()", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: first");

        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.Select(line => line.Message).ShouldBe(["first"]);
    }

    [Fact]
    public async Task when_the_query_names_an_account_level_and_text_then_only_matching_entries_are_returned()
    {
        File(
            "cloudsync-20261115.log",
            "2026-11-15 09:00:00.000 +00:00 [Debug] [AccountIdHash=aaa] Cat: listing photos",
            "2026-11-15 09:00:01.000 +00:00 [Warning] [AccountIdHash=aaa] Cat: listing throttled",
            "2026-11-15 09:00:02.000 +00:00 [Warning] [AccountIdHash=bbb] Cat: listing throttled",
            "2026-11-15 09:00:03.000 +00:00 [Warning] Cat: no account");

        var lines = await ReadAsync(LogQueryFactory.Create(Option.Some("aaa"), LogLevel.Warning, "throttled"));

        lines.Select(line => line.Message).ShouldBe(["listing throttled"]);
    }

    [Fact]
    public async Task when_a_file_is_being_written_while_it_is_read_then_the_read_still_works()
    {
        File("cloudsync-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: live");
        using var writer = fileSystem.File.Open($"{Folder}/cloudsync-20261115.log", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

        var lines = await ReadAsync(LogQueryFactory.CreateEverything());

        lines.Select(line => line.Message).ShouldBe(["live"]);
    }

    [Fact]
    public async Task when_the_read_is_cancelled_then_it_stops()
    {
        File("cloudsync-20261115.log", "2026-11-15 09:00:00.000 +00:00 [Information] Cat: one");
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        var read = async () =>
        {
            await foreach (var line in Reader().ReadAsync(LogQueryFactory.CreateEverything(), source.Token))
                _ = line;
        };

        await Should.ThrowAsync<OperationCanceledException>(read);
    }

    private sealed class FixedLogFileLocator(IDirectoryInfo folder) : ILogFileLocator
    {
        public IDirectoryInfo GetLogFolder() => folder;
    }
}
