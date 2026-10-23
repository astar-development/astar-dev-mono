using System.Text.RegularExpressions;

namespace AStarDev.OneDriveSyncClient.TestsUnit.Localization;

public sealed partial class GivenTheAxamlViews
{
    [GeneratedRegex(@"\b(?<attribute>Text|Content|Header|Title|PlaceholderText|Watermark|ToolTip\.Tip|Description|Label|OnContent|OffContent)=""(?<value>[^{""][^""]*)""")]
    private static partial Regex LiteralDisplayText();

    private static string FindAppDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AStarDev.slnx")))
            directory = directory.Parent;

        return Path.Combine(directory!.FullName, "apps", "desktop", "AStarDev.OneDriveSyncClient");
    }

    [Fact]
    public void when_axaml_views_are_scanned_then_no_literal_display_text_is_found()
    {
        string appDirectory = FindAppDirectory();
        string themesDirectory = Path.Combine(appDirectory, "Themes");

        var offenders = Directory.EnumerateFiles(appDirectory, "*.axaml", SearchOption.AllDirectories)
            .Where(file => !file.StartsWith(themesDirectory, StringComparison.Ordinal))
            .SelectMany(file => LiteralDisplayText().Matches(File.ReadAllText(file))
                .Where(match => match.Groups["value"].Value.Any(char.IsLetter))
                .Select(match => $"{Path.GetRelativePath(appDirectory, file)}: {match.Groups["attribute"].Value}=\"{match.Groups["value"].Value}\""))
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
