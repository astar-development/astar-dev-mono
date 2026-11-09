using AStarDev.OneDriveSyncClient.Infrastructure.Shell;
using AStarDev.OneDriveSyncClient.TestsUnit.TestHelpers;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Shell;

public sealed class GivenAFileManagerService
{
    private readonly RecordingProcessLauncher processLauncher = new();

    private FileManagerService CreateSut() => new(processLauncher);

    [Fact]
    public void when_a_folder_is_opened_then_one_process_is_started()
    {
        CreateSut().OpenFolder("/home/user/Documents");

        processLauncher.Started.ShouldHaveSingleItem();
    }

    [Fact]
    public void when_the_path_contains_spaces_then_it_is_passed_as_a_single_argument()
    {
        CreateSut().OpenFolder("/home/user/My Documents/Sub Folder");

        processLauncher.Started.ShouldHaveSingleItem().ArgumentList.ShouldBe(["/home/user/My Documents/Sub Folder"]);
    }

    [Fact]
    public void when_the_path_is_given_then_the_raw_arguments_string_is_not_used()
    {
        CreateSut().OpenFolder("/home/user/My Documents");

        processLauncher.Started.ShouldHaveSingleItem().Arguments.ShouldBeEmpty();
    }

    [Fact]
    public void when_a_folder_is_opened_then_the_shell_is_not_used()
    {
        CreateSut().OpenFolder("/home/user/Documents");

        processLauncher.Started.ShouldHaveSingleItem().UseShellExecute.ShouldBeFalse();
    }

    [Fact]
    public void when_a_folder_is_opened_then_the_file_name_is_the_platform_opener()
    {
        CreateSut().OpenFolder("/home/user/Documents");

        processLauncher.Started.ShouldHaveSingleItem().FileName.ShouldBe(RecordingProcessLauncher.ExpectedPlatformOpener());
    }
}
