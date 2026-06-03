using AStar.Dev.OneDrive.Sync.Client.Domain;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Data.Entities;

public sealed class GivenAnAccountSyncConfig
{
    private static readonly LocalSyncPath SyncPath = LocalSyncPath.Restore("/home/user/OneDrive");

    [Fact]
    public void when_created_then_conflict_policy_is_set_correctly()
    {
        var config = AccountSyncConfigFactory.Create(ConflictPolicy.LastWriteWins, SyncPath);

        config.ConflictPolicy.ShouldBe(ConflictPolicy.LastWriteWins);
    }

    [Fact]
    public void when_created_then_local_sync_path_is_set_correctly()
    {
        var config = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, SyncPath);

        config.LocalSyncPath.ShouldBe(SyncPath);
    }

    [Fact]
    public void when_two_instances_have_same_values_then_they_are_equal()
    {
        var first = AccountSyncConfigFactory.Create(ConflictPolicy.KeepBoth, SyncPath);
        var second = AccountSyncConfigFactory.Create(ConflictPolicy.KeepBoth, SyncPath);

        first.ShouldBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_conflict_policies_then_they_are_not_equal()
    {
        var first = AccountSyncConfigFactory.Create(ConflictPolicy.LocalWins, SyncPath);
        var second = AccountSyncConfigFactory.Create(ConflictPolicy.RemoteWins, SyncPath);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_two_instances_have_different_local_sync_paths_then_they_are_not_equal()
    {
        var otherPath = LocalSyncPath.Restore("/home/user/Other");
        var first = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, SyncPath);
        var second = AccountSyncConfigFactory.Create(ConflictPolicy.Ignore, otherPath);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_default_is_requested_then_conflict_policy_is_ignore()
    {
        var config = AccountSyncConfigFactory.Default;

        config.ConflictPolicy.ShouldBe(ConflictPolicy.Ignore);
    }

    [Fact]
    public void when_default_is_requested_then_local_sync_path_value_is_empty_string()
    {
        var config = AccountSyncConfigFactory.Default;

        config.LocalSyncPath.Value.ShouldBe(string.Empty);
    }
}
