namespace AStar.Dev.File.App.TestsUnit;

public class GivenApplicationMetadata
{
    [Fact]
    public void when_application_name_is_read_then_it_returns_the_expected_value()
        => ApplicationMetadata.ApplicationName.ShouldBe("AStar.Dev.File.App");

    [Fact]
    public void when_application_folder_is_read_then_it_returns_the_expected_value()
        => ApplicationMetadata.ApplicationFolder.ShouldBe("astar.dev.file.app");

    [Fact]
    public void when_application_name_hyphenated_is_read_then_it_returns_the_expected_value()
        => ApplicationMetadata.ApplicationNameHyphenated.ShouldBe("astar-dev-file-app");

    [Fact]
    public void when_application_name_dotted_is_read_then_it_returns_the_expected_value()
        => ApplicationMetadata.ApplicationNameDotted.ShouldBe("astar.dev.file.app");

    [Fact]
    public void when_application_log_name_is_read_then_it_returns_the_expected_value()
        => ApplicationMetadata.ApplicationLogName.ShouldBe("astar-dev-file-app.log");
}
