using System.Text.RegularExpressions;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Localisation;

public sealed partial class GivenNoHardcodedStringsInUiFiles
{
    private static readonly string _appRoot = FindAppRoot();

    [GeneratedRegex(@"(?<!\w)(?:Text|Title)=""(?!\{)(?!\s*"")([^""]{2,})""")]
    private static partial Regex HardcodedTextPattern();

    [GeneratedRegex(@"""[^""]*—[^""]*[Cc]oming [Ss]oon[^""]*""")]
    private static partial Regex ComingSoonLiteralPattern();

    [Fact]
    public void when_all_axaml_files_are_scanned_then_none_contain_hardcoded_text_or_title_values()
    {
        var violations = Directory
            .EnumerateFiles(_appRoot, "*.axaml", SearchOption.AllDirectories)
            .SelectMany(ScanAxamlFile)
            .ToList();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void when_all_viewmodel_files_are_scanned_then_none_contain_hardcoded_coming_soon_strings()
    {
        var violations = Directory
            .EnumerateFiles(_appRoot, "*ViewModel.cs", SearchOption.AllDirectories)
            .SelectMany(ScanViewModelFile)
            .ToList();

        violations.ShouldBeEmpty();
    }

    private static IEnumerable<string> ScanAxamlFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (IsDesignTimeOrCommentLine(line))
                continue;

            if (HardcodedTextPattern().IsMatch(line))
                yield return $"{filePath}:{i + 1} — {line.Trim()}";
        }
    }

    private static IEnumerable<string> ScanViewModelFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (ComingSoonLiteralPattern().IsMatch(line))
                yield return $"{filePath}:{i + 1} — {line.Trim()}";
        }
    }

    private static bool IsDesignTimeOrCommentLine(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("<!--", StringComparison.Ordinal)
            || trimmed.StartsWith("d:", StringComparison.Ordinal)
            || trimmed.StartsWith("<d:", StringComparison.Ordinal);
    }

    private static string FindAppRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0)
            {
                return Path.Combine(dir.FullName, "apps", "desktop", "AStar.Dev.OneDriveSync");
            }

            dir = dir.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
