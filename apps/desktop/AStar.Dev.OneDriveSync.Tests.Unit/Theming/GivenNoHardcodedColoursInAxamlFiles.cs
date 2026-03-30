using System.Text.RegularExpressions;

namespace AStar.Dev.OneDriveSync.Tests.Unit.Theming;

public sealed partial class GivenNoHardcodedColoursInAxamlFiles
{
    private static readonly string _axamlRoot = FindAxamlRoot();

    [GeneratedRegex(@"(?<![a-zA-Z])#[0-9A-Fa-f]{3,8}\b", RegexOptions.None)]
    private static partial Regex HardcodedColourPattern();

    [Fact]
    public void when_all_axaml_files_are_scanned_then_none_contain_hardcoded_colour_values()
    {
        var violations = Directory
            .EnumerateFiles(_axamlRoot, "*.axaml", SearchOption.AllDirectories)
            .SelectMany(file => ScanFile(file))
            .ToList();

        violations.ShouldBeEmpty();
    }

    private static IEnumerable<string> ScanFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (IsResourceDictionaryDefinition(line))
                continue;

            if (HardcodedColourPattern().IsMatch(line))
                yield return $"{filePath}:{i + 1} — {line.Trim()}";
        }
    }

    private static bool IsResourceDictionaryDefinition(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("<Color ", StringComparison.Ordinal)
            || trimmed.StartsWith("<SolidColorBrush ", StringComparison.Ordinal);
    }

    private static string FindAxamlRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0)
                return Path.Combine(dir.FullName, "apps", "desktop", "AStar.Dev.OneDriveSync");

            dir = dir.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
