using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncConflict
{
    [Fact]
    public void when_created_then_id_is_unique()
    {
        var conflict1 = new SyncConflict();
        var conflict2 = new SyncConflict();

        conflict1.Id.ShouldNotBe(conflict2.Id);
    }

    [Fact]
    public void when_created_then_state_is_pending()
    {
        var conflict = new SyncConflict();

        conflict.State.ShouldBe(ConflictState.Pending);
    }

    [Fact]
    public void when_created_then_resolution_is_null()
    {
        var conflict = new SyncConflict();

        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void when_created_then_detected_at_is_approximately_now()
    {
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-100);

        var conflict = new SyncConflict();

        conflict.DetectedAt.ShouldBeGreaterThanOrEqualTo(before);
        conflict.DetectedAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.AddMilliseconds(100));
    }

    [Fact]
    public void when_created_then_resolved_at_is_null()
    {
        var conflict = new SyncConflict();

        conflict.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public void when_all_properties_are_set_then_they_are_preserved()
    {
        var id = Guid.NewGuid();
        var remote = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789"));
        string relativePath = "Documents/report.pdf";
        string localPath = "/home/jason/Documents/report.pdf";
        var now = DateTimeOffset.UtcNow;

        var conflict = new SyncConflict
        {
            Id = id,
            Remote = remote,
            Target = SyncFileTargetFactory.Create(localPath, relativePath),
            Snapshot = ConflictSnapshotFactory.Create(now.AddHours(-1), 1024, now, 2048),
            DetectedAt = now
        };

        conflict.Id.ShouldBe(id);
        conflict.Remote.AccountId.Id.ShouldBe("account-123");
        conflict.Remote.FolderId.Id.ShouldBe("folder-456");
        conflict.Remote.RemoteItemId.Id.ShouldBe("item-789");
        conflict.Target.RelativePath.ShouldBe(relativePath);
        conflict.Target.LocalPath.ShouldBe(localPath);
        conflict.Snapshot.LocalSize.ShouldBe(1024);
        conflict.Snapshot.RemoteSize.ShouldBe(2048);
    }

    [Fact]
    public void when_target_is_set_then_local_path_and_relative_path_are_accessible_via_target()
    {
        var target = SyncFileTargetFactory.Create("/home/user/docs/report.pdf", "docs/report.pdf");

        var conflict = new SyncConflict { Target = target };

        conflict.Target.LocalPath.ShouldBe("/home/user/docs/report.pdf");
        conflict.Target.RelativePath.ShouldBe("docs/report.pdf");
    }

    [Fact]
    public void when_resolved_then_state_and_resolution_and_resolved_at_are_set()
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
    public void when_skipped_then_state_is_skipped()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.State = ConflictState.Skipped;

        conflict.State.ShouldBe(ConflictState.Skipped);
    }

    [Theory]
    [InlineData(ConflictState.Pending)]
    [InlineData(ConflictState.Resolved)]
    [InlineData(ConflictState.Skipped)]
    public void when_state_is_set_then_it_is_preserved(ConflictState state)
    {
        var conflict = new SyncConflict { State = state };

        conflict.State.ShouldBe(state);
    }

    [Fact]
    public void when_local_size_is_large_then_it_is_preserved()
    {
        long largeSize = 1_073_741_824L;

        var conflict = new SyncConflict { Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.MinValue, largeSize, DateTimeOffset.MinValue, 0L) };

        conflict.Snapshot.LocalSize.ShouldBe(largeSize);
    }

    [Fact]
    public void when_local_is_older_than_remote_then_version_conflict_is_tracked()
    {
        var now = DateTimeOffset.UtcNow;

        var conflict = new SyncConflict
        {
            Snapshot = ConflictSnapshotFactory.Create(now.AddHours(-2), 1024, now, 2048)
        };

        conflict.Snapshot.LocalModified.ShouldBeLessThan(conflict.Snapshot.RemoteModified);
        conflict.Snapshot.LocalSize.ShouldNotBe(conflict.Snapshot.RemoteSize);
    }

    [Fact]
    public void when_resolution_is_not_set_then_it_is_null()
    {
        var conflict = new SyncConflict { State = ConflictState.Pending };

        conflict.Resolution.ShouldBeNull();
    }

    [Fact]
    public void when_resolution_is_set_then_it_is_not_null()
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
