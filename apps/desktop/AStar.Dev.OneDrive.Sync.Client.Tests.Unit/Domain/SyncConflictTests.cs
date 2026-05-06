using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public class SyncConflictTests
{
    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveUniqueId()
    {
        var conflict1 = new SyncConflict();
        var conflict2 = new SyncConflict();

        conflict1.Id.ShouldNotBe(conflict2.Id);
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHavePendingState()
    {
        var conflict = new SyncConflict();

        conflict.State.ShouldBe(ConflictState.Pending);
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveNoResolution()
    {
        var conflict = new SyncConflict();

        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldHaveCurrentDetectedTime()
    {
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-100);

        var conflict = new SyncConflict();

        conflict.DetectedAt.ShouldBeGreaterThanOrEqualTo(before);
        conflict.DetectedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.AddMilliseconds(100));
    }

    [Fact]
    public void SyncConflict_NewInstance_ShouldNotBeResolved()
    {
        var conflict = new SyncConflict();

        conflict.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_CanSetPropertiesViaInit()
    {
        var id = Guid.NewGuid();
        string accountId = "account-123";
        string folderId = "folder-456";
        string remoteItemId = "item-789";
        string relativePath = "Documents/report.pdf";
        string localPath = "/home/jason/Documents/report.pdf";
        var now = DateTimeOffset.UtcNow;

        var conflict = new SyncConflict
        {
            Id = id,
            AccountId = accountId,
            FolderId = folderId,
            RemoteItemId = remoteItemId,
            RelativePath = relativePath,
            LocalPath = localPath,
            LocalModified = now.AddHours(-1),
            RemoteModified = now,
            LocalSize = 1024,
            RemoteSize = 2048,
            DetectedAt = now
        };

        conflict.Id.ShouldBe(id);
        conflict.AccountId.ShouldBe(accountId);
        conflict.FolderId.ShouldBe(folderId);
        conflict.RemoteItemId.ShouldBe(remoteItemId);
        conflict.RelativePath.ShouldBe(relativePath);
        conflict.LocalPath.ShouldBe(localPath);
        conflict.LocalSize.ShouldBe(1024);
        conflict.RemoteSize.ShouldBe(2048);
    }

    [Fact]
    public void SyncConflict_CanBeResolved()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.State = ConflictState.Resolved;
        conflict.Resolution = ConflictPolicy.LastWriteWins;
        conflict.ResolvedAt = DateTimeOffset.UtcNow;

        conflict.State.ShouldBe(ConflictState.Resolved);
        conflict.Resolution.ShouldBe(ConflictPolicy.LastWriteWins);
        _ = conflict.ResolvedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SyncConflict_CanBeSkipped()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.State = ConflictState.Skipped;

        conflict.State.ShouldBe(ConflictState.Skipped);
    }

    [Theory]
    [InlineData(ConflictState.Pending)]
    [InlineData(ConflictState.Resolved)]
    [InlineData(ConflictState.Skipped)]
    public void SyncConflict_ShouldSupportAllStates(ConflictState state)
    {
        var conflict = new SyncConflict { State = state };

        conflict.State.ShouldBe(state);
    }

    [Fact]
    public void SyncConflict_LocalSizeCanBeLarge()
    {
        long largeSize = 1_073_741_824L;

        var conflict = new SyncConflict { LocalSize = largeSize };

        conflict.LocalSize.ShouldBe(largeSize);
    }

    [Fact]
    public void SyncConflict_CanTrackVersionConflict()
    {
        var now = DateTimeOffset.UtcNow;

        var conflict = new SyncConflict
        {
            LocalModified = now.AddHours(-2),
            RemoteModified = now,
            LocalSize = 1024,
            RemoteSize = 2048
        };

        conflict.LocalModified.ShouldBeLessThan(conflict.RemoteModified);
        conflict.LocalSize.ShouldNotBe(conflict.RemoteSize);
    }

    [Fact]
    public void SyncConflict_WithoutResolution_ShouldHaveNullResolution()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void SyncConflict_WithResolution_ShouldHaveNonNullResolution()
    {
        var conflict = new SyncConflict
        {
            State = ConflictState.Resolved,
            Resolution = ConflictPolicy.LocalWins
        };

        _ = conflict.Resolution.ShouldNotBeNull();
        conflict.Resolution.ShouldBe(ConflictPolicy.LocalWins);
    }
}
