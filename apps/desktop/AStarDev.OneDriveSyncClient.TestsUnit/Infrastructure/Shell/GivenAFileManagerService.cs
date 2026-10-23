using AStarDev.OneDriveSyncClient.Infrastructure.Shell;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Infrastructure.Shell;

public sealed class GivenAFileManagerService
{
    [Fact]
    public void when_the_path_contains_spaces_then_it_is_passed_as_a_single_argument()
    {
        var startInfo = FileManagerService.CreateStartInfo("/home/user/My Documents/Sub Folder");

        startInfo.ArgumentList.ShouldBe(["/home/user/My Documents/Sub Folder"]);
    }

    [Fact]
    public void when_the_path_is_given_then_the_raw_arguments_string_is_not_used()
    {
        var startInfo = FileManagerService.CreateStartInfo("/home/user/My Documents");

        startInfo.Arguments.ShouldBeEmpty();
    }

    [Fact]
    public void when_the_start_info_is_created_then_the_shell_is_not_used()
    {
        var startInfo = FileManagerService.CreateStartInfo("/home/user/Documents");

        startInfo.UseShellExecute.ShouldBeFalse();
    }

    [Fact]
    public void when_the_start_info_is_created_then_the_file_name_is_the_platform_opener()
    {
        var startInfo = FileManagerService.CreateStartInfo("/home/user/Documents");

        startInfo.FileName.ShouldBe(FileManagerService.GetOpener());
    }
}
