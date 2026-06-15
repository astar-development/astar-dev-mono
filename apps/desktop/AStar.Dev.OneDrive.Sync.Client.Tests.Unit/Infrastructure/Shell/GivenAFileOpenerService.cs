using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Shell;

public sealed class GivenAFileOpenerService
{
    private readonly FileOpenerService sut = new();

    [Fact]
    public void when_file_does_not_exist_then_open_file_does_not_throw()
    {
        Should.NotThrow(() => sut.OpenFile("/nonexistent/path/to/file.txt"));
    }

    [Fact]
    public void when_path_is_empty_string_then_open_file_does_not_throw()
    {
        Should.NotThrow(() => sut.OpenFile(string.Empty));
    }

    [Fact]
    public void when_on_linux_then_opener_is_xdg_open()
    {
        if (!OperatingSystem.IsLinux())
            return;

        FileOpenerService.GetOpener().ShouldBe("xdg-open");
    }

    [Fact]
    public void when_on_macos_then_opener_is_open()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        FileOpenerService.GetOpener().ShouldBe("open");
    }

    [Fact]
    public void when_on_windows_then_opener_is_explorer()
    {
        if (!OperatingSystem.IsWindows())
            return;

        FileOpenerService.GetOpener().ShouldBe("explorer");
    }

    [Fact]
    public void when_getting_opener_then_a_non_empty_string_is_returned()
    {
        FileOpenerService.GetOpener().ShouldNotBeNullOrEmpty();
    }
}
