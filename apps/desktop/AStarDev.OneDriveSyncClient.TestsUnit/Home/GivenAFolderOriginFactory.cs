using AStarDev.OneDriveSyncClient.Home;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Home;

public sealed class GivenAFolderOriginFactory
{
    [Fact]
    public void when_remote_only_is_created_then_remote_only_case_is_returned() =>
        FolderOriginFactory.CreateRemoteOnly().ShouldBeOfType<FolderOrigin.RemoteOnly>();

    [Fact]
    public void when_local_only_is_created_then_local_only_case_is_returned() =>
        FolderOriginFactory.CreateLocalOnly().ShouldBeOfType<FolderOrigin.LocalOnly>();

    [Fact]
    public void when_both_is_created_then_both_case_is_returned() =>
        FolderOriginFactory.CreateBoth().ShouldBeOfType<FolderOrigin.Both>();
}
