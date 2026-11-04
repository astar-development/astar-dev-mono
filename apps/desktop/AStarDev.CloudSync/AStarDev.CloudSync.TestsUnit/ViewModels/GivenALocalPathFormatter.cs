using AStarDev.CloudSync.ViewModels;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.TestsUnit.ViewModels;

public class GivenALocalPathFormatter
{
    [Fact]
    public void when_the_path_is_under_the_home_folder_then_the_home_folder_is_shown_as_a_tilde()
    {
        var formatter = CreateFormatter("/home/user");

        formatter.Format("/home/user/CloudSync/OneDrive/Work/Documents").ShouldBe("~/CloudSync/OneDrive/Work/Documents");
    }

    [Fact]
    public void when_the_path_is_the_home_folder_then_it_is_shown_as_a_tilde()
    {
        var formatter = CreateFormatter("/home/user");

        formatter.Format("/home/user").ShouldBe("~");
    }

    [Fact]
    public void when_the_home_folder_has_a_trailing_separator_then_paths_under_it_are_still_shortened()
    {
        var formatter = CreateFormatter("/home/user/");

        formatter.Format("/home/user/Documents").ShouldBe("~/Documents");
    }

    [Theory]
    [InlineData("/srv/shared/Documents")]
    [InlineData("/home/username/Documents")]
    [InlineData("/home")]
    public void when_the_path_is_not_under_the_home_folder_then_it_is_unchanged(string path)
    {
        var formatter = CreateFormatter("/home/user");

        formatter.Format(path).ShouldBe(path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void when_the_path_is_blank_then_it_is_unchanged(string path)
    {
        var formatter = CreateFormatter("/home/user");

        formatter.Format(path).ShouldBe(path);
    }

    [Fact]
    public void when_there_is_no_home_folder_then_the_path_is_unchanged()
    {
        var formatter = new LocalPathFormatter(new FixedHomeFolder(Option.None<DirectoryInfo>()));

        formatter.Format("/home/user/Documents").ShouldBe("/home/user/Documents");
    }

    [Fact]
    public void when_the_path_differs_from_the_home_folder_only_by_case_then_it_is_shortened_only_where_paths_ignore_case()
    {
        var formatter = CreateFormatter("/home/user");
        string expected = OperatingSystem.IsLinux() ? "/HOME/USER/Documents" : "~/Documents";

        formatter.Format("/HOME/USER/Documents").ShouldBe(expected);
    }

    private static LocalPathFormatter CreateFormatter(string homeFolder) => new(new FixedHomeFolder(Option.Some(new DirectoryInfo(homeFolder))));
}
