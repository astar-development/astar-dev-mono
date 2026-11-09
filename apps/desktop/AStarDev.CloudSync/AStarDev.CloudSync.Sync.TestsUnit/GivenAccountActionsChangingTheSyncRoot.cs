using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.IO.Abstractions;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public sealed class GivenAccountActionsChangingTheSyncRoot : IDisposable
{
    private const string Personal = "personal-key";
    private const string Work = "work-key";

    private readonly AccountServices services = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task when_the_folder_exists_then_it_is_saved_as_the_accounts_root()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        _ = services.FileSystem.Directory.CreateDirectory("/data/sync");

        var result = await services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/data/sync"), CancellationToken);

        result.Match(root => (object)root.Directory.FullName, rejection => rejection).ShouldBe("/data/sync");
        services.SyncRoots.Roots.Select(entry => (entry.AccountId, entry.Root.Directory.FullName)).ShouldBe([(accountId, "/data/sync")]);
    }

    [Fact]
    public async Task when_the_folder_is_missing_but_its_parent_exists_then_it_is_created_and_saved()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        _ = services.FileSystem.Directory.CreateDirectory("/data");

        var result = await services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/data/sync"), CancellationToken);

        result.Match(_ => true, _ => false).ShouldBeTrue();
        services.FileSystem.Directory.Exists("/data/sync").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_parent_of_a_missing_folder_is_missing_too_then_it_is_unavailable_and_nothing_is_created_or_saved()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);

        var result = await services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/mnt/usb/sync"), CancellationToken);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateUnavailable());
        services.FileSystem.Directory.Exists("/mnt/usb").ShouldBeFalse();
        services.SyncRoots.SetAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_folder_cannot_be_created_for_lack_of_permission_then_it_is_not_writable()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var parent = Substitute.For<IDirectoryInfo>();
        parent.Exists.Returns(true);
        var candidate = Substitute.For<IDirectoryInfo>();
        candidate.Exists.Returns(false);
        candidate.Parent.Returns(parent);
        candidate.When(folder => folder.Create()).Throw(new UnauthorizedAccessException());

        var result = await services.Actions.ChangeSyncRootAsync(accountId, candidate, CancellationToken);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateNotWritable());
        services.SyncRoots.SetAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_folder_crosses_another_accounts_root_then_it_is_rejected_and_a_folder_created_for_it_is_removed_again()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var otherId = await services.AddAsync(Work, CancellationToken);
        _ = services.FileSystem.Directory.CreateDirectory("/data/work");
        _ = await services.SyncRoots.SetAsync(otherId, services.FileSystem.DirectoryInfo.New("/data/work"), CancellationToken);

        var result = await services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/data/work/inner"), CancellationToken);

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateInsideOf(otherId));
        services.FileSystem.Directory.Exists("/data/work/inner").ShouldBeFalse();
        services.FileSystem.Directory.Exists("/data/work").ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_rejected_folder_already_existed_then_it_is_left_in_place()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        var otherId = await services.AddAsync(Work, CancellationToken);
        _ = services.FileSystem.Directory.CreateDirectory("/data/work/inner");
        _ = await services.SyncRoots.SetAsync(otherId, services.FileSystem.DirectoryInfo.New("/data/work"), CancellationToken);

        _ = await services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/data/work/inner"), CancellationToken);

        services.FileSystem.Directory.Exists("/data/work/inner").ShouldBeTrue();
    }

    [Fact]
    public async Task when_a_pass_keeps_running_then_the_change_reports_sync_still_running_after_ten_seconds_and_changes_nothing()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        _ = services.FileSystem.Directory.CreateDirectory("/data/sync");
        using var running = ((SyncLockAttempt.Acquired)services.SyncLock.TryAcquire(accountId)).Lease;

        var result = await services.AdvanceUntilAsync(services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/data/sync"), CancellationToken));

        result.Match(root => (object)root, rejection => rejection).ShouldBe(SyncRootRejectionFactory.CreateSyncStillRunning());
        services.SyncRoots.SetAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task when_the_change_finishes_then_the_accounts_lease_is_released()
    {
        var accountId = await services.AddAsync(Personal, CancellationToken);
        _ = services.FileSystem.Directory.CreateDirectory("/data/sync");
        _ = await services.Actions.ChangeSyncRootAsync(accountId, services.FileSystem.DirectoryInfo.New("/data/sync"), CancellationToken);

        var next = services.SyncLock.TryAcquire(accountId);

        next.ShouldBeOfType<SyncLockAttempt.Acquired>();
    }

    public void Dispose() => services.Dispose();
}
