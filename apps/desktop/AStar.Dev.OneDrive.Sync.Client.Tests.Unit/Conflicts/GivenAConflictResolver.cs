using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Data.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Conflicts;

public sealed class GivenAConflictResolver
{
    private static readonly MockFileSystem mockFileSystem = new();

    [Fact]
    public void when_resolving_with_ignore_policy_then_skip_is_returned()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.Ignore, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.Skip);
    }

    [Fact]
    public void when_resolving_with_local_wins_policy_then_use_local_is_returned()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LocalWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void when_resolving_with_remote_wins_policy_then_use_remote_is_returned()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.RemoteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void when_resolving_with_keep_both_policy_then_keep_both_is_returned()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.KeepBoth, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void when_resolving_with_last_write_wins_and_local_is_newer_then_use_local_is_returned()
    {
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var localModified = DateTimeOffset.UtcNow;

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void when_resolving_with_last_write_wins_and_remote_is_newer_then_use_remote_is_returned()
    {
        var localModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var remoteModified = DateTimeOffset.UtcNow;

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void when_resolving_with_last_write_wins_and_same_times_then_use_local_is_returned()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, timestamp, timestamp);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void when_resolving_with_last_write_wins_and_local_is_one_second_newer_then_use_local_is_returned()
    {
        var remoteModified = DateTimeOffset.UtcNow;
        var localModified = remoteModified.AddSeconds(1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void when_resolving_with_last_write_wins_and_remote_is_one_second_newer_then_use_remote_is_returned()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = localModified.AddSeconds(1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void when_resolving_with_last_write_wins_and_old_vs_new_timestamps_then_comparison_is_correct()
    {
        var remoteModified = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var localModified = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.LocalWins)]
    [InlineData(ConflictPolicy.RemoteWins)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    public void when_resolving_with_any_policy_then_a_valid_outcome_is_returned(ConflictPolicy policy)
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        var outcome = ConflictResolver.Resolve(policy, localModified, remoteModified);

        outcome.ShouldNotBe((ConflictOutcome)999);
        outcome.ShouldBeOneOf(
            ConflictOutcome.Skip,
            ConflictOutcome.UseLocal,
            ConflictOutcome.UseRemote,
            ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void when_making_keep_both_name_then_valid_filename_is_generated()
    {
        string localPath = "/home/jason/Documents/report.docx";
        var localModified = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, mockFileSystem);

        result.ShouldContain("report");
        result.ShouldContain("docx");
        result.ShouldContain("local");
        result.ShouldContain("2024-01-15");
        result.ShouldContain("14-32");
    }

    [Fact]
    public void when_making_keep_both_name_with_spaces_in_path_then_extension_is_preserved()
    {
        string localPath = "/home/jason/My Documents/My Report.docx";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, mockFileSystem);

        result.ShouldContain("My Report");
        result.ShouldEndWith(".docx");
    }

    [Fact]
    public void when_making_keep_both_name_with_no_extension_then_name_is_still_generated()
    {
        string localPath = "/home/jason/Documents/README";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, mockFileSystem);

        result.ShouldContain("README");
        result.ShouldContain("local");
    }

    [Fact]
    public void when_making_keep_both_name_with_multiple_dots_then_only_last_extension_is_removed()
    {
        string localPath = "/home/jason/file.tar.gz";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, mockFileSystem);

        result.ShouldContain("file.tar");
        result.ShouldEndWith(".gz");
    }

    [Fact]
    public void when_making_keep_both_name_with_different_times_then_unique_paths_are_generated()
    {
        string localPath = "/home/jason/Documents/report.docx";
        var time1 = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 1, 16, 14, 32, 0, TimeSpan.Zero);

        string result1 = ConflictResolver.MakeKeepBothName(localPath, time1, mockFileSystem);
        string result2 = ConflictResolver.MakeKeepBothName(localPath, time2, mockFileSystem);

        result1.ShouldNotBe(result2);
        result1.ShouldContain("2024-01-15");
        result2.ShouldContain("2024-01-16");
    }

    [Fact]
    public void when_making_keep_both_name_then_output_is_in_same_directory()
    {
        string localPath = "/home/jason/Documents/report.docx";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, mockFileSystem);

        string? resultDir = Path.GetDirectoryName(result);
        string? originalDir = Path.GetDirectoryName(localPath);
        resultDir.ShouldBe(originalDir);
    }
}
