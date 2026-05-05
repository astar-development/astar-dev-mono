using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncFileMetadata
{
    private const long FileSize = 4096L;
    private static readonly DateTimeOffset RemoteModified = new(2026, 3, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void when_created_then_file_size_is_set_correctly()
    {
        var metadata = SyncFileMetadataFactory.Create(FileSize, RemoteModified);

        metadata.FileSize.ShouldBe(FileSize);
    }

    [Fact]
    public void when_created_then_remote_modified_is_set_correctly()
    {
        var metadata = SyncFileMetadataFactory.Create(FileSize, RemoteModified);

        metadata.RemoteModified.ShouldBe(RemoteModified);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = SyncFileMetadataFactory.Create(FileSize, RemoteModified);
        var second = SyncFileMetadataFactory.Create(FileSize, RemoteModified);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_file_sizes_then_they_are_not_equal()
    {
        var first = SyncFileMetadataFactory.Create(FileSize, RemoteModified);
        var second = SyncFileMetadataFactory.Create(8192L, RemoteModified);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_remote_modified_dates_then_they_are_not_equal()
    {
        var first = SyncFileMetadataFactory.Create(FileSize, RemoteModified);
        var second = SyncFileMetadataFactory.Create(FileSize, RemoteModified.AddHours(1));

        first.ShouldNotBe(second);
    }
}
