using System.Text.RegularExpressions;

namespace AStarDev.ControlDb.TestsUnit.Utilities;

internal static partial class RegexHelpers
{
    [GeneratedRegex(@"\d{1,4}-\d{1,2}-\d{1,2}T\d{1,2}:\d{1,2}:\d{1,2}\.\d{1,7}\+00:00")]
    public static partial Regex DateTimeFormatRegex();
}
