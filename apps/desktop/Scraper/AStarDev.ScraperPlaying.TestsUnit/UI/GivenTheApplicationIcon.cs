using System.Text;
using System.Xml.Linq;
using AStarDev.ScraperPlaying.UI;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenTheApplicationIcon
{
    private const string IconPath = "/Assets/astar.png";

    [Fact]
    public void when_the_application_is_built_then_the_icon_image_is_embedded_as_an_avalonia_resource()
    {
        using var resources = typeof(MainWindow).Assembly.GetManifestResourceStream("!AvaloniaResources");

        using var reader = new StreamReader(resources!, Encoding.UTF8);

        reader.ReadToEnd().ShouldContain(IconPath);
    }

    [Theory]
    [InlineData("StartupErrorWindow")]
    [InlineData("MainWindow")]
    [InlineData("ConfigurationEditorWindow")]
    [InlineData("ConfirmationWindow")]
    [InlineData("TagsEditorWindow")]
    public void when_a_window_is_declared_then_it_uses_the_application_icon(string windowName)
    {
        var xamlPath = Path.Combine(FindRepositoryRoot(), "apps", "desktop", "Scraper", "AStarDev.ScraperPlaying", "UI", $"{windowName}.axaml");

        XDocument.Load(xamlPath).Root!.Attribute("Icon")!.Value.ShouldBe(IconPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AStarDev.slnx")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
