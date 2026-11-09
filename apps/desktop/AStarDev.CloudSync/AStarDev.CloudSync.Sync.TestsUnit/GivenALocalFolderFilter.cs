using System.IO.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenALocalFolderFilter
{
    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    [Fact]
    public void when_the_folder_is_an_ordinary_folder_then_it_is_visible()
    {
        var folder = fileSystem.Directory.CreateDirectory("/root/Photos");

        Filter(folder).ShouldBe(LocalFolderVisibilityFactory.CreateVisible());
    }

    [Fact]
    public void when_the_folder_name_starts_with_a_dot_then_it_is_skipped_as_dot_prefixed()
    {
        var folder = fileSystem.Directory.CreateDirectory("/root/.git");

        Filter(folder).ShouldBe(LocalFolderVisibilityFactory.CreateSkipped(SkipReasonFactory.CreateDotPrefixed()));
    }

    [Fact]
    public void when_the_folder_has_the_hidden_attribute_then_it_is_skipped_as_hidden()
    {
        var folder = fileSystem.Directory.CreateDirectory("/root/Cache");
        folder.Attributes |= FileAttributes.Hidden;

        Filter(folder).ShouldBe(LocalFolderVisibilityFactory.CreateSkipped(SkipReasonFactory.CreateHidden()));
    }

    [Fact]
    public void when_the_folder_is_a_symbolic_link_then_it_is_skipped_as_a_symbolic_link()
    {
        _ = fileSystem.Directory.CreateDirectory("/elsewhere");
        _ = fileSystem.Directory.CreateDirectory("/root");
        _ = fileSystem.Directory.CreateSymbolicLink("/root/link", "/elsewhere");

        Filter(fileSystem.DirectoryInfo.New("/root/link")).ShouldBe(LocalFolderVisibilityFactory.CreateSkipped(SkipReasonFactory.CreateSymbolicLink()));
    }

    [Theory]
    [InlineData("report.3f2a9c1e-5b7d-4e6a-8c0d-1a2b3c4d5e6f.download")]
    [InlineData("report.3f2a9c1e5b7d4e6a8c0d1a2b3c4d5e6f.download")]
    [InlineData("REPORT.3F2A9C1E-5B7D-4E6A-8C0D-1A2B3C4D5E6F.DOWNLOAD")]
    public void when_the_folder_is_an_app_temporary_download_item_then_it_is_skipped_as_app_temporary(string name)
    {
        var folder = fileSystem.Directory.CreateDirectory($"/root/{name}");

        Filter(folder).ShouldBe(LocalFolderVisibilityFactory.CreateSkipped(SkipReasonFactory.CreateAppTemporary()));
    }

    [Theory]
    [InlineData("downloads")]
    [InlineData("report.download")]
    [InlineData("report.not-a-guid.download")]
    public void when_the_folder_only_looks_a_little_like_a_temporary_item_then_it_is_visible(string name)
    {
        var folder = fileSystem.Directory.CreateDirectory($"/root/{name}");

        Filter(folder).ShouldBe(LocalFolderVisibilityFactory.CreateVisible());
    }

    private static LocalFolderVisibility Filter(IDirectoryInfo folder) => new LocalFolderFilter().ShouldSkip(folder);
}
