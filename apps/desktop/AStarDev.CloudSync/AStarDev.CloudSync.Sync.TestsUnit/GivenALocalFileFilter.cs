using System.IO.Abstractions;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.Sync.TestsUnit;

public class GivenALocalFileFilter
{
    private readonly MockFileSystem fileSystem = new(options => options.SimulatingOperatingSystem(SimulationMode.Linux));

    [Theory]
    [InlineData("Report.docx")]
    [InlineData(".gitignore")]
    [InlineData(".env")]
    [InlineData("notes.download")]
    [InlineData("budget.tmp")]
    [InlineData("Report.not-a-guid.download")]
    [InlineData("desktop.ini.bak")]
    [InlineData("~Report.docx")]
    public void when_the_file_is_a_user_file_then_it_is_visible(string name)
    {
        var file = CreateFile($"/root/{name}");

        Filter(file).ShouldBe(LocalFileVisibilityFactory.CreateVisible());
    }

    [Fact]
    public void when_the_file_has_the_hidden_attribute_then_it_is_skipped_as_hidden()
    {
        var windows = new MockFileSystem(options => options.SimulatingOperatingSystem(SimulationMode.Windows));
        _ = windows.Directory.CreateDirectory("C:\\root");
        windows.File.WriteAllText("C:\\root\\cache.bin", "x");
        var file = windows.FileInfo.New("C:\\root\\cache.bin");
        file.Attributes |= FileAttributes.Hidden;

        Filter(file).ShouldBe(LocalFileVisibilityFactory.CreateSkipped(FileSkipReasonFactory.CreateHidden()));
    }

    [Theory]
    [InlineData("report.3f2a9c1e-5b7d-4e6a-8c0d-1a2b3c4d5e6f.download")]
    [InlineData("report.3f2a9c1e5b7d4e6a8c0d1a2b3c4d5e6f.download")]
    [InlineData("REPORT.3F2A9C1E-5B7D-4E6A-8C0D-1A2B3C4D5E6F.DOWNLOAD")]
    public void when_the_file_is_an_app_temp_download_then_it_is_skipped_as_temporary(string name)
    {
        var file = CreateFile($"/root/{name}");

        Filter(file).ShouldBe(LocalFileVisibilityFactory.CreateSkipped(FileSkipReasonFactory.CreateTemporary()));
    }

    [Theory]
    [InlineData("~$Report.docx")]
    [InlineData("~$")]
    public void when_the_file_is_an_office_lock_file_then_it_is_skipped_as_an_office_lock(string name)
    {
        var file = CreateFile($"/root/{name}");

        Filter(file).ShouldBe(LocalFileVisibilityFactory.CreateSkipped(FileSkipReasonFactory.CreateOfficeLock()));
    }

    [Theory]
    [InlineData("desktop.ini")]
    [InlineData("Desktop.INI")]
    [InlineData("Thumbs.db")]
    [InlineData("thumbs.DB")]
    public void when_the_file_is_an_operating_system_file_then_it_is_skipped_as_a_system_file(string name)
    {
        var file = CreateFile($"/root/{name}");

        Filter(file).ShouldBe(LocalFileVisibilityFactory.CreateSkipped(FileSkipReasonFactory.CreateSystemFile()));
    }

    [Fact]
    public void when_the_file_is_a_symbolic_link_then_it_is_skipped_as_a_symbolic_link()
    {
        _ = CreateFile("/elsewhere/real.txt");
        _ = fileSystem.Directory.CreateDirectory("/root");
        _ = fileSystem.File.CreateSymbolicLink("/root/link.txt", "/elsewhere/real.txt");

        Filter(fileSystem.FileInfo.New("/root/link.txt")).ShouldBe(LocalFileVisibilityFactory.CreateSkipped(FileSkipReasonFactory.CreateSymbolicLink()));
    }

    [Fact]
    public void when_the_file_does_not_exist_then_it_is_visible_so_the_scan_reports_it_as_unreadable_not_deleted()
    {
        var file = fileSystem.FileInfo.New("/root/vanished.txt");

        Filter(file).ShouldBe(LocalFileVisibilityFactory.CreateVisible());
    }

    private static LocalFileVisibility Filter(IFileInfo file) => new LocalFileFilter().ShouldSkip(file);

    private IFileInfo CreateFile(string path)
    {
        _ = fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(path)!);
        fileSystem.File.WriteAllText(path, "x");

        return fileSystem.FileInfo.New(path);
    }
}
