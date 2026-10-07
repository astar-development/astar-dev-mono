using AStarDev.FunctionalParadigm;
using AStarDev.OneDriveSyncClient.Home;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Home;

public sealed class GivenAMergedFolderFactory
{
    [Fact]
    public void when_created_then_all_values_are_preserved()
    {
        var origin = FolderOriginFactory.CreateBoth();

        var folder = MergedFolderFactory.Create("Photos", "/Photos", Option.Some("r1"), Option.Some("root"), origin, FolderSyncState.Included);

        folder.Name.ShouldBe("Photos");
        folder.RemotePath.ShouldBe("/Photos");
        folder.RemoteId.ShouldBe(Option.Some("r1"));
        folder.ParentId.ShouldBe(Option.Some("root"));
        folder.Origin.ShouldBe(origin);
        folder.SyncState.ShouldBe(FolderSyncState.Included);
    }
}
