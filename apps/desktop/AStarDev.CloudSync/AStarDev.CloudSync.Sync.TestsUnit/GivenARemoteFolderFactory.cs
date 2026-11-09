namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenARemoteFolderFactory
{
    [Fact]
    public void when_a_folder_is_created_then_its_name_is_trimmed()
    {
        var folder = RemoteFolderFactory.Create(RemoteIds.Of("folder-1"), "  Documents ", RemoteIds.Of("root"), RemoteFolderChildrenFactory.CreateNoChildFolders());

        (folder.Id, folder.Name, folder.ParentId, folder.Children).ShouldBe((RemoteIds.Of("folder-1"), "Documents", RemoteIds.Of("root"), RemoteFolderChildrenFactory.CreateNoChildFolders()));
    }

    [Fact]
    public void when_a_folder_has_no_name_then_the_name_is_empty()
    {
        var folder = RemoteFolderFactory.Create(RemoteIds.Of("folder-1"), null, RemoteIds.Of("root"), RemoteFolderChildrenFactory.CreateMayHaveChildFolders());

        folder.Name.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_folder_is_printed_then_its_name_is_redacted()
    {
        var folder = RemoteFolderFactory.Create(RemoteIds.Of("folder-1"), "Jane Doe Tax Returns", RemoteIds.Of("root"), RemoteFolderChildrenFactory.CreateNoChildFolders());

        folder.ToString().ShouldNotContain("Jane");
    }

    [Theory]
    [InlineData(5, "MayHaveChildFolders")]
    [InlineData(1, "MayHaveChildFolders")]
    [InlineData(0, "NoChildFolders")]
    [InlineData(-1, "NoChildFolders")]
    public void when_children_come_from_the_child_count_then_only_a_positive_count_may_have_child_folders(int childCount, string expected)
    {
        var children = RemoteFolderChildrenFactory.CreateFromChildCount(childCount);

        children.GetType().Name.ShouldBe(expected);
    }
}
