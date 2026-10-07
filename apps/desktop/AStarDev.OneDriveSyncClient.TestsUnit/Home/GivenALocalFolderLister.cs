using AStarDev.OneDriveSyncClient.Home;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Home;

public sealed class GivenALocalFolderLister
{
    private const string SyncRoot = "/sync-root";

    private static LocalFolderLister CreateSut(MockFileSystem fileSystem) => new(fileSystem);

    [Fact]
    public void when_the_sync_root_has_child_folders_then_their_names_are_returned()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync-root/Photos");
        fileSystem.Directory.CreateDirectory("/sync-root/Drafts");

        var result = CreateSut(fileSystem).ListChildFolderNames(SyncRoot, "/");

        result.ShouldBe(["Drafts", "Photos"], ignoreOrder: true);
    }

    [Fact]
    public void when_remote_path_is_empty_then_the_sync_root_is_listed()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync-root/Photos");

        var result = CreateSut(fileSystem).ListChildFolderNames(SyncRoot, string.Empty);

        result.ShouldBe(["Photos"]);
    }

    [Fact]
    public void when_remote_path_is_nested_then_the_matching_local_folder_is_listed()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync-root/Documents/Reports");
        fileSystem.Directory.CreateDirectory("/sync-root/Other");

        var result = CreateSut(fileSystem).ListChildFolderNames(SyncRoot, "/Documents");

        result.ShouldBe(["Reports"]);
    }

    [Fact]
    public void when_the_folder_contains_files_then_only_folders_are_returned()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync-root/Photos");
        fileSystem.File.WriteAllText("/sync-root/readme.txt", "text");

        var result = CreateSut(fileSystem).ListChildFolderNames(SyncRoot, "/");

        result.ShouldBe(["Photos"]);
    }

    [Fact]
    public void when_a_folder_name_starts_with_a_dot_then_it_is_skipped()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/sync-root/.git");
        fileSystem.Directory.CreateDirectory("/sync-root/Photos");

        var result = CreateSut(fileSystem).ListChildFolderNames(SyncRoot, "/");

        result.ShouldBe(["Photos"]);
    }

    [Fact]
    public void when_a_folder_is_hidden_then_it_is_skipped()
    {
        var fileSystem = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        fileSystem.Directory.CreateDirectory(@"C:\sync-root\Secret");
        fileSystem.DirectoryInfo.New(@"C:\sync-root\Secret").Attributes = FileAttributes.Hidden | FileAttributes.Directory;
        fileSystem.Directory.CreateDirectory(@"C:\sync-root\Photos");

        var result = CreateSut(fileSystem).ListChildFolderNames(@"C:\sync-root", "/");

        result.ShouldBe(["Photos"]);
    }

    [Fact]
    public void when_the_local_folder_does_not_exist_then_result_is_empty()
    {
        var fileSystem = new MockFileSystem();

        var result = CreateSut(fileSystem).ListChildFolderNames(SyncRoot, "/Missing");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_sync_root_is_blank_then_result_is_empty()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.Directory.CreateDirectory("/Photos");

        var result = CreateSut(fileSystem).ListChildFolderNames(string.Empty, "/");

        result.ShouldBeEmpty();
    }
}
