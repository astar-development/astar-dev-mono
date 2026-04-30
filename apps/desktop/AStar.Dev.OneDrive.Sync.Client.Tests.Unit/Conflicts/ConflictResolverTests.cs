using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class ConflictResolverTests
{
    private static readonly MockFileSystem MockFs = new();

    [Fact]
    public void Resolve_WithIgnorePolicy_ShouldReturnSkip()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.Ignore, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.Skip);
    }

    [Fact]
    public void Resolve_WithLocalWinsPolicy_ShouldReturnUseLocal()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LocalWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithRemoteWinsPolicy_ShouldReturnUseRemote()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.RemoteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithKeepBothPolicy_ShouldReturnKeepBoth()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.KeepBoth, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_LocalIsNewer_ShouldReturnUseLocal()
    {
        var remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var localModified = DateTimeOffset.UtcNow;

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_RemoteIsNewer_ShouldReturnUseRemote()
    {
        var localModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        var remoteModified = DateTimeOffset.UtcNow;

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_SameTimes_ShouldReturnUseLocal()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, timestamp, timestamp);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_LocalJustOneSecondNewer_ShouldReturnUseLocal()
    {
        var remoteModified = DateTimeOffset.UtcNow;
        var localModified = remoteModified.AddSeconds(1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_RemoteJustOneSecondNewer_ShouldReturnUseRemote()
    {
        var localModified = DateTimeOffset.UtcNow;
        var remoteModified = localModified.AddSeconds(1);

        var outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_OldTimestampsVsNewTimestamps_ShouldCompareCorrectly()
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
    public void Resolve_ShouldHandleAllPolicies(ConflictPolicy policy)
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
    public void MakeKeepBothName_ShouldGenerateValidFilename()
    {
        string localPath = "/home/jason/Documents/report.docx";
        var localModified = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, MockFs);

        result.ShouldContain("report");
        result.ShouldContain("docx");
        result.ShouldContain("local");
        result.ShouldContain("2024-01-15");
        result.ShouldContain("14-32");
    }

    [Fact]
    public void MakeKeepBothName_WithPathContainingSpaces_ShouldPreserveExtension()
    {
        string localPath = "/home/jason/My Documents/My Report.docx";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, MockFs);

        result.ShouldContain("My Report");
        result.ShouldEndWith(".docx");
    }

    [Fact]
    public void MakeKeepBothName_WithFileHavingNoExtension_ShouldStillGenerateName()
    {
        string localPath = "/home/jason/Documents/README";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, MockFs);

        result.ShouldContain("README");
        result.ShouldContain("local");
    }

    [Fact]
    public void MakeKeepBothName_WithMultipleDots_ShouldOnlyRemoveLastExtension()
    {
        string localPath = "/home/jason/file.tar.gz";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, MockFs);

        result.ShouldContain("file.tar");
        result.ShouldEndWith(".gz");
    }

    [Fact]
    public void MakeKeepBothName_GeneratesIncreasinglyUniquePaths()
    {
        string localPath = "/home/jason/Documents/report.docx";
        var time1 = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 1, 16, 14, 32, 0, TimeSpan.Zero);

        string result1 = ConflictResolver.MakeKeepBothName(localPath, time1, MockFs);
        string result2 = ConflictResolver.MakeKeepBothName(localPath, time2, MockFs);

        result1.ShouldNotBe(result2);
        result1.ShouldContain("2024-01-15");
        result2.ShouldContain("2024-01-16");
    }

    [Fact]
    public void MakeKeepBothName_OutputPathIsOnSameDirectory()
    {
        string localPath = "/home/jason/Documents/report.docx";
        var localModified = DateTimeOffset.UtcNow;

        string result = ConflictResolver.MakeKeepBothName(localPath, localModified, MockFs);

        string? resultDir = Path.GetDirectoryName(result);
        string? originalDir = Path.GetDirectoryName(localPath);
        resultDir.ShouldBe(originalDir);
    }
}
