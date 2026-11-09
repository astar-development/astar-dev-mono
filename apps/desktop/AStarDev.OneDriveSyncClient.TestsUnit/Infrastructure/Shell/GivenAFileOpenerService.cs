using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Shell;

public sealed class GivenAFileOpenerService
{
    private const string ExistingFileName = "report with spaces.txt";

    private readonly RecordingProcessLauncher processLauncher = new();
    private readonly MockFileSystem fileSystem = new();
    private readonly string existingFilePath;

    public GivenAFileOpenerService()
    {
        existingFilePath = fileSystem.Path.GetFullPath(ExistingFileName);
        _ = fileSystem.Initialize().WithFile(existingFilePath).Which(file => file.HasStringContent("data"));
    }

    private FileOpenerService CreateSut() => new(processLauncher, fileSystem);

    [Theory]
    [InlineData("/nonexistent/path/to/file.txt")]
    [InlineData("/nonexistent/path with spaces/to file.txt")]
    [InlineData("")]
    public void when_the_file_does_not_exist_then_no_process_is_started(string localPath)
    {
        CreateSut().OpenFile(localPath);

        processLauncher.Started.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_file_exists_only_on_the_real_disk_then_no_process_is_started()
    {
        var realFilePath = typeof(GivenAFileOpenerService).Assembly.Location;

        CreateSut().OpenFile(realFilePath);

        processLauncher.Started.ShouldBeEmpty();
    }

    [Fact]
    public void when_on_linux_and_only_a_differently_cased_file_exists_then_no_process_is_started()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux only");

        CreateSut().OpenFile(existingFilePath.ToUpperInvariant());

        processLauncher.Started.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_file_exists_then_the_platform_opener_is_started()
    {
        CreateSut().OpenFile(existingFilePath);

        processLauncher.Started.ShouldHaveSingleItem().FileName.ShouldBe(RecordingProcessLauncher.ExpectedPlatformOpener());
    }

    [Fact]
    public void when_the_file_exists_then_its_path_is_passed_as_a_single_argument()
    {
        CreateSut().OpenFile(existingFilePath);

        processLauncher.Started.ShouldHaveSingleItem().ArgumentList.ShouldBe([existingFilePath]);
    }

    [Fact]
    public void when_the_file_exists_then_the_shell_is_not_used()
    {
        CreateSut().OpenFile(existingFilePath);

        processLauncher.Started.ShouldHaveSingleItem().UseShellExecute.ShouldBeFalse();
    }

    [Fact]
    public void when_on_linux_then_the_opener_is_xdg_open()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "Linux only");

        CreateSut().OpenFile(existingFilePath);

        processLauncher.Started.ShouldHaveSingleItem().FileName.ShouldBe("xdg-open");
    }

    [Fact]
    public void when_on_macos_then_the_opener_is_open()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "macOS only");

        CreateSut().OpenFile(existingFilePath);

        processLauncher.Started.ShouldHaveSingleItem().FileName.ShouldBe("open");
    }

    [Fact]
    public void when_on_windows_then_the_opener_is_explorer()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only");

        CreateSut().OpenFile(existingFilePath);

        processLauncher.Started.ShouldHaveSingleItem().FileName.ShouldBe("explorer");
    }
}
