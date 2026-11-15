using AStarDev.CloudSync.Logging;
using Testably.Abstractions.Testing;

namespace AStarDev.CloudSync.TestsUnit;

public sealed class GivenALogFolderResolver
{
    [Fact]
    public void when_the_platform_is_linux_then_the_folder_is_under_local_share_in_the_home_folder()
    {
        var environment = LogEnvironmentFactory.Create(LogPlatformFactory.CreateLinux(), "/home/sam", "/home/sam/.local/share");

        LogFolderResolver.Resolve(environment).ShouldBe("/home/sam/.local/share/AStarDev.CloudSync/logs");
    }

    [Fact]
    public void when_the_platform_is_windows_then_the_folder_is_under_local_app_data()
    {
        var environment = LogEnvironmentFactory.Create(LogPlatformFactory.CreateWindows(), @"C:\Users\Sam", @"C:\Users\Sam\AppData\Local");

        LogFolderResolver.Resolve(environment).ShouldBe(@"C:\Users\Sam\AppData\Local\AStarDev.CloudSync\logs");
    }

    [Fact]
    public void when_the_platform_is_macos_then_the_folder_is_under_library_logs_in_the_home_folder()
    {
        var environment = LogEnvironmentFactory.Create(LogPlatformFactory.CreateMacOs(), "/Users/sam", "/Users/sam/Library/Application Support");

        LogFolderResolver.Resolve(environment).ShouldBe("/Users/sam/Library/Logs/AStarDev.CloudSync");
    }

    [Fact]
    public void when_the_home_folder_ends_with_a_separator_then_the_folder_has_no_doubled_separator()
    {
        var environment = LogEnvironmentFactory.Create(LogPlatformFactory.CreateLinux(), "/home/sam/", null);

        LogFolderResolver.Resolve(environment).ShouldBe("/home/sam/.local/share/AStarDev.CloudSync/logs");
    }

    [Fact]
    public void when_the_locator_is_asked_then_it_points_at_the_folder_the_writer_uses()
    {
        var fileSystem = new MockFileSystem();
        var locator = new CloudSyncLogFileLocator(fileSystem);

        locator.GetLogFolder().FullName.ShouldBe(fileSystem.DirectoryInfo.New(CloudSyncLogging.FolderPath()).FullName);
    }
}
