using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenAConflictSnapshot
{
    private static readonly DateTimeOffset LocalModified = new(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RemoteModified = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private const long LocalSize = 1024L;
    private const long RemoteSize = 2048L;

    [Fact]
    public void when_created_then_local_modified_is_preserved()
    {
        var snapshot = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);

        snapshot.LocalModified.ShouldBe(LocalModified);
    }

    [Fact]
    public void when_created_then_local_size_is_preserved()
    {
        var snapshot = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);

        snapshot.LocalSize.ShouldBe(LocalSize);
    }

    [Fact]
    public void when_created_then_remote_modified_is_preserved()
    {
        var snapshot = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);

        snapshot.RemoteModified.ShouldBe(RemoteModified);
    }

    [Fact]
    public void when_created_then_remote_size_is_preserved()
    {
        var snapshot = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);

        snapshot.RemoteSize.ShouldBe(RemoteSize);
    }

    [Fact]
    public void when_two_snapshots_have_same_values_then_they_are_equal()
    {
        var first = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);
        var second = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_snapshots_differ_by_local_size_then_they_are_not_equal()
    {
        var first = ConflictSnapshotFactory.Create(LocalModified, LocalSize, RemoteModified, RemoteSize);
        var second = ConflictSnapshotFactory.Create(LocalModified, LocalSize + 1, RemoteModified, RemoteSize);

        first.ShouldNotBe(second);
    }
}
