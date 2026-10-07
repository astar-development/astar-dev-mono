using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Home;
using AStarDev.OneDriveSyncClient.Infrastructure.Graph;
using AStarDev.OneDriveSyncClient.Localization;
using Microsoft.Extensions.Logging;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Home;

public sealed class GivenAFolderTreeNodeViewModelFactory
{
    private static Func<CancellationToken, Task<string>> TokenFactory => _ => Task.FromResult("token-abc");

    private static FolderTreeNodeViewModelFactory CreateSut() => CreateSut(new MockFileSystem());

    private static FolderTreeNodeViewModelFactory CreateSut(MockFileSystem fileSystem) => new(Substitute.For<IGraphService>(), Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>(), new LocalFolderLister(fileSystem));

    private static FolderTreeNode BuildNode() => new(Id: "folder-1", Name: "Documents", ParentId: Option.None<string>(), AccountId: "account-1", RemotePath: "/Documents", SyncState: FolderSyncState.Included, HasChildren: true);

    [Fact]
    public void when_create_is_called_then_the_node_details_are_projected_onto_the_view_model()
    {
        var sut = CreateSut();

        var viewModel = sut.Create(BuildNode(), TokenFactory, new DriveId("drive-1"), _ => null, string.Empty);

        viewModel.RemoteId.ShouldBe(Option.Some("folder-1"));
        viewModel.Name.ShouldBe("Documents");
        viewModel.RemotePath.ShouldBe("/Documents");
        viewModel.SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_create_is_called_then_the_view_model_is_a_root_node()
    {
        var sut = CreateSut();

        var viewModel = sut.Create(BuildNode(), TokenFactory, new DriveId("drive-1"), _ => null, string.Empty);

        viewModel.Depth.ShouldBe(0);
    }

    [Fact]
    public void when_create_root_level_is_called_with_a_local_only_folder_then_it_is_included_and_local_only()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync/Drafts");
        var sut = CreateSut(fileSystem);

        var roots = sut.CreateRootLevel([], "account-1", "/sync", TokenFactory, new DriveId("drive-1"), _ => null);

        var root = roots.ShouldHaveSingleItem();
        root.Name.ShouldBe("Drafts");
        root.IsLocalOnly.ShouldBeTrue();
        root.SyncState.ShouldBe(FolderSyncState.Included);
        root.RemotePath.ShouldBe("/Drafts");
    }

    [Fact]
    public void when_create_root_level_is_called_with_a_remote_only_folder_then_it_is_excluded_by_default()
    {
        var sut = CreateSut(new MockFileSystem());

        var roots = sut.CreateRootLevel([new DriveFolder("r1", "Photos", Option.None<string>())], "account-1", "/sync", TokenFactory, new DriveId("drive-1"), _ => null);

        var root = roots.ShouldHaveSingleItem();
        root.IsLocalOnly.ShouldBeFalse();
        root.SyncState.ShouldBe(FolderSyncState.Excluded);
        root.RemoteId.ShouldBe(Option.Some("r1"));
    }

    [Fact]
    public void when_create_root_level_is_called_with_a_folder_on_both_sides_then_a_single_node_is_returned()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync/Photos");
        var sut = CreateSut(fileSystem);

        var roots = sut.CreateRootLevel([new DriveFolder("r1", "Photos", Option.None<string>())], "account-1", "/sync", TokenFactory, new DriveId("drive-1"), _ => null);

        roots.ShouldHaveSingleItem().IsLocalOnly.ShouldBeFalse();
    }

    [Fact]
    public void when_create_root_level_is_called_with_a_persisted_rule_then_the_rule_state_is_used()
    {
        var sut = CreateSut(new MockFileSystem());

        var roots = sut.CreateRootLevel([new DriveFolder("r1", "Photos", Option.None<string>())], "account-1", "/sync", TokenFactory, new DriveId("drive-1"), _ => FolderSyncState.Included);

        roots.ShouldHaveSingleItem().SyncState.ShouldBe(FolderSyncState.Included);
    }

    [Fact]
    public void when_create_root_level_is_called_with_no_folders_then_the_result_is_empty()
    {
        var sut = CreateSut(new MockFileSystem());

        sut.CreateRootLevel([], "account-1", "/sync", TokenFactory, new DriveId("drive-1"), _ => null).ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_created_root_is_expanded_then_local_children_below_the_sync_root_are_merged()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync/Photos/Holidays");
        var graphService = Substitute.For<IGraphService>();
        graphService.GetChildFoldersAsync(Arg.Any<Func<CancellationToken, Task<string>>>(), Arg.Any<DriveId>(), "r1", Arg.Any<CancellationToken>())
            .Returns(new Ok<List<DriveFolder>, string>([]));
        var sut = new FolderTreeNodeViewModelFactory(graphService, Substitute.For<ILogger<FolderTreeNodeViewModel>>(), Substitute.For<ILocalizationService>(), new LocalFolderLister(fileSystem));
        var root = sut.CreateRootLevel([new DriveFolder("r1", "Photos", Option.None<string>())], "account-1", "/sync", TokenFactory, new DriveId("drive-1"), _ => null).Single();

        await root.ToggleExpandCommand.ExecuteAsync(null);

        root.Children.Single().Name.ShouldBe("Holidays");
    }
}
