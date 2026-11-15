using System.Reflection;
using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.TestsUnit.Localisation;

internal static partial class LocalisationSources
{
    [GeneratedRegex("(?<![$@])\"([A-Z][A-Za-z0-9]*(?:\\.[A-Za-z0-9]+)+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex KeyShapedLiteral();

    [GeneratedRegex("\\b\\w*(?:Text|Content|Title|Watermark|ToolTip|Description|Subtitle|Caption|Label)\\w*\\s*(?:=|=>)\\s*\\$?\"([^\"]*[A-Za-z]{3,}[^\"]*)\"", RegexOptions.CultureInvariant)]
    private static partial Regex UserFacingAssignment();

    [GeneratedRegex("\\b(?:Text|Content|Title|Header|Watermark|ToolTip\\.Tip|Label|Description|PlaceholderText)=\"(?!\\{)([^\"]*[A-Za-z]{2,}[^\"]*)\"", RegexOptions.CultureInvariant)]
    private static partial Regex AxamlLiteral();

    internal static DirectoryInfo CloudSyncFolder() =>
        new(typeof(LocalisationSources).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "CloudSyncSourceFolder").Value ?? throw new InvalidOperationException("The build records where the CloudSync projects are."));

    internal static IReadOnlyList<(string File, string Text)> ProductionFiles(string extension) =>
    [
        .. CloudSyncFolder().GetDirectories("AStarDev.CloudSync*")
            .Where(folder => !folder.Name.Contains("Test", StringComparison.Ordinal))
            .SelectMany(folder => folder.EnumerateFiles($"*{extension}", SearchOption.AllDirectories))
            .Where(file => !file.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !file.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(file => (file.Name, File.ReadAllText(file.FullName))),
    ];

    internal static IEnumerable<string> KeyLiteralsIn(string source, IReadOnlySet<string> keyPrefixes) =>
        KeyShapedLiteral().Matches(source).Select(match => match.Groups[1].Value).Where(key => keyPrefixes.Contains(key.Split('.')[0]));

    internal static IEnumerable<string> UserFacingLiteralsIn(string source) =>
        source.Split('\n').Where(line => !line.TrimStart().StartsWith('[') && !line.Contains("CommandText", StringComparison.Ordinal) && !line.Contains("Variable", StringComparison.Ordinal) && !line.TrimStart().StartsWith("//", StringComparison.Ordinal)).SelectMany(line => UserFacingAssignment().Matches(line).Select(match => match.Groups[1].Value)).Where(text => text.Contains(' ') || char.IsUpper(text[0]) && !text.Contains('.'));

    internal static IEnumerable<string> AxamlLiteralsIn(string axaml) => AxamlLiteral().Matches(axaml).Select(match => match.Groups[1].Value);
}
