using System.Reflection;
using System.Text.RegularExpressions;

namespace AStarDev.CloudSync.TestsUnit.Localisation;

internal static partial class LocalisationSources
{
    [GeneratedRegex("(?<![$@])\"([A-Z][A-Za-z0-9]*(?:\\.[A-Za-z0-9]+)+)\"", RegexOptions.CultureInvariant)]
    private static partial Regex KeyShapedLiteral();

    [GeneratedRegex("\\b(?:Text|Content|Title|Header|Watermark|ToolTip|Description|Subtitle|Caption|Label)\\w*\\s*(?:=|=>)\\s*\\$?\"([^\"]*[A-Za-z]{3,}[^\"]*)\"", RegexOptions.CultureInvariant)]
    private static partial Regex UserFacingAssignment();

    [GeneratedRegex("\\b(?:Text|Content|Title|Header|Watermark|ToolTip\\.Tip|Label|Description|PlaceholderText)=\"([^\"{][^\"]*[A-Za-z]{2,}[^\"]*)\"", RegexOptions.CultureInvariant)]
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

    internal static IEnumerable<string> KeyLiteralsIn(string source, IReadOnlySet<string> keyPrefixes) => throw new NotImplementedException($"{source} {keyPrefixes}");

    internal static IEnumerable<string> UserFacingLiteralsIn(string source) => throw new NotImplementedException($"{source}");

    internal static IEnumerable<string> AxamlLiteralsIn(string axaml) => throw new NotImplementedException($"{axaml}");
}
