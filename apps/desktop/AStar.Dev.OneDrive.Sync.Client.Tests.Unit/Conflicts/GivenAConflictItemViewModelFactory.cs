using AStar.Dev.OneDrive.Sync.Client.Conflicts;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Sync;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using AccountId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.AccountId;
using OneDriveItemId = AStar.Dev.OneDrive.Sync.Client.Data.Entities.OneDriveItemId;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Conflicts;

public sealed class GivenAConflictItemViewModelFactory
{
    private static SyncConflict BuildConflict() => new()
    {
        Remote   = RemoteItemRefFactory.Create(new AccountId("account-123"), new OneDriveFolderId("folder-456"), new OneDriveItemId("item-789")),
        Target   = SyncFileTargetFactory.Create("/home/user/docs/report.pdf", "docs/report.pdf"),
        Snapshot = ConflictSnapshotFactory.Create(DateTimeOffset.UtcNow.AddHours(-1), 1024L, DateTimeOffset.UtcNow, 2048L),
    };

    [Fact]
    public void when_create_is_called_then_the_conflict_is_projected_onto_the_view_model()
    {
        var sut = new ConflictItemViewModelFactory(Substitute.For<ISyncService>(), Substitute.For<ILocalizationService>());
        var conflict = BuildConflict();

        var viewModel = sut.Create(conflict);

        viewModel.Id.ShouldBe(conflict.Id);
        viewModel.AccountId.ShouldBe("account-123");
    }
}
