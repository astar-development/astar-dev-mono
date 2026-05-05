using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Domain;

public sealed class GivenASyncFileTarget
{
    private const string LocalPath = "/home/user/Documents/report.pdf";
    private const string RelativePath = "Documents/report.pdf";

    [Fact]
    public void when_created_then_local_path_is_set_correctly()
    {
        var target = SyncFileTargetFactory.Create(LocalPath, RelativePath);

        target.LocalPath.ShouldBe(LocalPath);
    }

    [Fact]
    public void when_created_then_relative_path_is_set_correctly()
    {
        var target = SyncFileTargetFactory.Create(LocalPath, RelativePath);

        target.RelativePath.ShouldBe(RelativePath);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = SyncFileTargetFactory.Create(LocalPath, RelativePath);
        var second = SyncFileTargetFactory.Create(LocalPath, RelativePath);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_local_paths_then_they_are_not_equal()
    {
        var first = SyncFileTargetFactory.Create(LocalPath, RelativePath);
        var second = SyncFileTargetFactory.Create("/home/user/Other/report.pdf", RelativePath);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_relative_paths_then_they_are_not_equal()
    {
        var first = SyncFileTargetFactory.Create(LocalPath, RelativePath);
        var second = SyncFileTargetFactory.Create(LocalPath, "Other/report.pdf");

        first.ShouldNotBe(second);
    }
}
