using System.Globalization;

namespace AStarDev.CloudSync.Sync;

/// <inheritdoc />
public sealed class KeepBothNamer : IKeepBothNamer
{
    private const string StampFormat = "yyyy-MM-dd'T'HHmmss'Z'";
    private const char ExtensionSeparator = '.';
    private const int FirstCounter = 2;

    /// <inheritdoc />
    public string Name(string original, DateTimeOffset utc, IReadOnlyCollection<string> existingNames)
    {
        var name = original ?? string.Empty;
        var dot = name.LastIndexOf(ExtensionSeparator);
        var stem = dot > 0 ? name[..dot] : name;
        var extension = dot > 0 ? name[dot..] : string.Empty;
        var stamp = utc.UtcDateTime.ToString(StampFormat, CultureInfo.InvariantCulture);
        var taken = new HashSet<string>(existingNames ?? [], StringComparer.OrdinalIgnoreCase);
        var plain = $"{stem}-({stamp}){extension}";

        return taken.Contains(plain) ? Enumerable.Range(FirstCounter, int.MaxValue - FirstCounter).Select(counter => $"{stem}-({stamp})-{counter}{extension}").First(candidate => !taken.Contains(candidate)) : plain;
    }
}
