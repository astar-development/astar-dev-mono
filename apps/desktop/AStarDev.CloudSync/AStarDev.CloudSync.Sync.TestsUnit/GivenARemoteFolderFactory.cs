using AStarDev.FunctionalParadigm;

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

    [Fact]
    public void when_a_folder_is_created_without_a_version_then_it_has_none()
    {
        var folder = RemoteFolderFactory.Create(RemoteIds.Of("folder-1"), "Documents", RemoteIds.Of("root"), RemoteFolderChildrenFactory.CreateNoChildFolders());

        folder.Version.Match(_ => true, () => false).ShouldBeFalse();
    }

    [Fact]
    public void when_a_folder_is_created_with_a_version_then_it_carries_the_entity_tag_and_modified_time()
    {
        var modified = new DateTimeOffset(2026, 11, 14, 8, 0, 0, TimeSpan.Zero);

        var folder = RemoteFolderFactory.Create(RemoteIds.Of("folder-1"), "  Documents ", RemoteIds.Of("root"), RemoteFolderChildrenFactory.CreateNoChildFolders(), RemoteFolderVersionFactory.Create(new EntityTag("fe1"), modified));

        folder.Name.ShouldBe("Documents");
        folder.Version.Match(version => (version.ETag.Value, version.LastModified), () => ("none", DateTimeOffset.MinValue)).ShouldBe(("fe1", modified));
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
