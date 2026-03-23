using System.Text.RegularExpressions;

namespace AStar.Dev.Test.Helpers.Unit;

internal sealed partial class ShouldlyExtensionsRegex
{
    [GeneratedRegex(@"\d{4}-[01]\d-[0-3]\dT[0-2]\d:[0-5]\d:[0-5]\d\.\d+([+-][0-2]\d:[0-5]\d|Z)")]
    public static partial Regex RegexIsoDate();

    [GeneratedRegex(@"\w{8}-\w{4}-\w{4}-\w{4}-\w{12}")]
    public static partial Regex RegexGuid();
}