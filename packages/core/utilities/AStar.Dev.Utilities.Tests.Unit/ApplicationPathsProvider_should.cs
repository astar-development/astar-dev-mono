namespace AStar.Dev.Utilities.Tests.Unit;

// ReSharper disable once InconsistentNaming
public class ApplicationPathsProvider_should
{
    [Fact]
    public void return_the_expected_application_directory()
    {
        "test-application-name".ApplicationDirectory()
            .ShouldEndWith("/.config/test-application-name");
    }
    [Fact]
    public void return_the_expected_logs_directory()
    {
        "test-application-name".LogsDirectory()
            .ShouldEndWith("/.local/share/test-application-name/logs");
    }
    [Fact]
    public void return_the_expected_users_directory()
    {
        "test-application-name".UserDirectory()
            .ShouldEndWith("/Documents/test-application-name/sync");
    }
}
