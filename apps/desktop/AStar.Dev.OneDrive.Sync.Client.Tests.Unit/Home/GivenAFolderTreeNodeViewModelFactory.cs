using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Domain;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Graph;
using AStar.Dev.OneDrive.Sync.Client.Localization;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Home;

public sealed class GivenAFolderTreeNodeViewModelFactory
{
    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("token-abc");

    private static FolderTreeNodeViewModelFactory CreateSut() => new(Substitute.For<IGraphService>(), Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>());

    private static FolderTreeNode BuildNode() => new(Id: "folder-1", Name: "Documents", ParentId: Option.None<string>(), AccountId: "account-1", RemotePath: "/Documents", SyncState: FolderSyncState.Included, HasChildren: true);

    [Fact]
    public void when_create_is_called_then_the_node_details_are_projected_onto_the_view_model()
    {
        var sut = CreateSut();

        var viewModel = sut.Create(BuildNode(), TokenFactory, new DriveId("drive-1"), _ => null);

        viewModel.Id.ShouldBe("folder-1");
        viewModel.Name.ShouldBe("Documents");
        viewModel.RemotePath.ShouldBe("/Documents");
        viewModel.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_create_is_called_then_the_view_model_is_a_root_node()
    {
        var sut = CreateSut();

        var viewModel = sut.Create(BuildNode(), TokenFactory, new DriveId("drive-1"), _ => null);

        viewModel.Depth.ShouldBe(0);
    }
}
