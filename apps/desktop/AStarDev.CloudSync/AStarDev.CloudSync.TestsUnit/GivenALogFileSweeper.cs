using AStarDev.CloudSync.Logging;
using Microsoft.Extensions.Time.Testing;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenALogFileSweeper
{
    private const string Folder = "/logs";

    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));

    private LogFileSweeper Create() => new(time);

    private void Write(string name, int daysOld)
    {
        _ = fileSystem.Directory.CreateDirectory(Folder);
        var path = $"{Folder}/{name}";
        fileSystem.File.WriteAllText(path, "entry");
        fileSystem.File.SetLastWriteTimeUtc(path, time.GetUtcNow().UtcDateTime.AddDays(-daysOld));
    }

    private string Remaining() => string.Join(",", fileSystem.Directory.EnumerateFiles(Folder).Select(fileSystem.Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal));

    [Fact]
    public void when_log_files_are_older_than_the_age_then_only_those_are_deleted()
    {
        Write("cloudsync-20261101.log", 13);
        Write("cloudsync-20261105.log", 9);
        Write("cloudsync-20261113.log", 1);

        var deleted = Create().DeleteOlderThan(fileSystem.DirectoryInfo.New(Folder), "cloudsync", TimeSpan.FromDays(10));

        (deleted, Remaining()).ShouldBe((1, "cloudsync-20261105.log,cloudsync-20261113.log"));
    }

    [Fact]
    public void when_other_files_are_old_then_they_are_left_alone()
    {
        Write("cloudsync-20261001.log", 40);
        Write("notes.txt", 40);
        Write("other-20261001.log", 40);

        var deleted = Create().DeleteOlderThan(fileSystem.DirectoryInfo.New(Folder), "cloudsync", TimeSpan.FromDays(10));

        (deleted, Remaining()).ShouldBe((1, "notes.txt,other-20261001.log"));
    }

    private void WriteSized(string name, int bytes, int daysOld)
    {
        _ = fileSystem.Directory.CreateDirectory(Folder);
        var path = $"{Folder}/{name}";
        fileSystem.File.WriteAllBytes(path, new byte[bytes]);
        fileSystem.File.SetLastWriteTimeUtc(path, time.GetUtcNow().UtcDateTime.AddDays(-daysOld));
    }

    [Fact]
    public void when_the_log_files_take_more_than_the_total_then_the_oldest_are_deleted_until_they_fit()
    {
        WriteSized("cloudsync-20261110.log", 40, 4);
        WriteSized("cloudsync-20261111.log", 40, 3);
        WriteSized("cloudsync-20261112.log", 40, 2);
        WriteSized("cloudsync-20261113.log", 40, 1);

        var deleted = LogFileSweeper.DeleteBeyondTotalSize(fileSystem.DirectoryInfo.New(Folder), "cloudsync", 100);

        (deleted, Remaining()).ShouldBe((2, "cloudsync-20261112.log,cloudsync-20261113.log"));
    }

    [Fact]
    public void when_the_log_files_fit_the_total_then_none_is_deleted()
    {
        WriteSized("cloudsync-20261112.log", 50, 2);
        WriteSized("cloudsync-20261113.log", 50, 1);

        var deleted = LogFileSweeper.DeleteBeyondTotalSize(fileSystem.DirectoryInfo.New(Folder), "cloudsync", 100);

        (deleted, Remaining()).ShouldBe((0, "cloudsync-20261112.log,cloudsync-20261113.log"));
    }

    [Fact]
    public void when_other_files_are_large_then_they_are_not_counted_or_deleted()
    {
        WriteSized("cloudsync-20261113.log", 50, 1);
        WriteSized("notes.txt", 500, 9);

        var deleted = LogFileSweeper.DeleteBeyondTotalSize(fileSystem.DirectoryInfo.New(Folder), "cloudsync", 100);

        (deleted, Remaining()).ShouldBe((0, "cloudsync-20261113.log,notes.txt"));
    }

    [Fact]
    public void when_the_folder_is_missing_then_the_total_size_sweep_deletes_nothing()
    {
        LogFileSweeper.DeleteBeyondTotalSize(fileSystem.DirectoryInfo.New("/missing"), "cloudsync", 100).ShouldBe(0);
    }

    [Fact]
    public void when_the_folder_does_not_exist_then_nothing_is_deleted_and_nothing_fails()
    {
        Create().DeleteOlderThan(fileSystem.DirectoryInfo.New("/missing"), "cloudsync", TimeSpan.FromDays(10)).ShouldBe(0);
    }

    [Fact]
    public void when_a_file_is_exactly_the_age_old_then_it_is_kept()
    {
        Write("cloudsync-20261104.log", 10);

        var deleted = Create().DeleteOlderThan(fileSystem.DirectoryInfo.New(Folder), "cloudsync", TimeSpan.FromDays(10));

        (deleted, Remaining()).ShouldBe((0, "cloudsync-20261104.log"));
    }
}
