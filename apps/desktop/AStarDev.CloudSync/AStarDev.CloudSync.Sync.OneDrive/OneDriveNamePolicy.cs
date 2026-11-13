using System.Buffers;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync.OneDrive;

/// <summary>The names, path lengths and file sizes OneDrive accepts. Linux and macOS allow some names OneDrive rejects, so those items are reported and skipped, never renamed.</summary>
internal sealed class OneDriveNamePolicy : IRemoteNamePolicy
{
    private const int MaxPathLength = 400;
    private const long MaxFileBytes = 250L * 1024 * 1024 * 1024;
    private const string TemporaryPrefix = "~$";
    private const string VersionMarker = "_vti_";

    private static readonly SearchValues<char> IllegalCharacters = SearchValues.Create("\"*:<>?/\\|");

    private static readonly HashSet<string> ReservedNames = new(
        ["CON", "PRN", "AUX", "NUL", ".lock", "desktop.ini", .. Enumerable.Range(0, 10).SelectMany(digit => new[] { $"COM{digit}", $"LPT{digit}" })],
        StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Result<Unit, InvalidRemoteName> Check(string name, RelativeSyncPath path, long size) => Violation(name, path, size).Match(
        reason => Result.Failure<Unit, InvalidRemoteName>(reason),
        () => Result.Success<Unit, InvalidRemoteName>(Unit.Instance));

    private static Option<InvalidRemoteName> Violation(string name, RelativeSyncPath path, long size) => name switch
    {
        _ when name.AsSpan().ContainsAny(IllegalCharacters) => Option.Some(InvalidRemoteNameFactory.CreateIllegalCharacter()),
        _ when name.Length != name.Trim().Length => Option.Some(InvalidRemoteNameFactory.CreateLeadingOrTrailingSpace()),
        _ when name.EndsWith('.') => Option.Some(InvalidRemoteNameFactory.CreateEndsWithDot()),
        _ when IsReserved(name) => Option.Some(InvalidRemoteNameFactory.CreateReservedName()),
        _ when (path.Value ?? string.Empty).Length > MaxPathLength => Option.Some(InvalidRemoteNameFactory.CreatePathTooLong()),
        _ when size > MaxFileBytes => Option.Some(InvalidRemoteNameFactory.CreateFileTooLarge()),
        _ => Option.None<InvalidRemoteName>(),
    };

    private static bool IsReserved(string name) => ReservedNames.Contains(name) || name.StartsWith(TemporaryPrefix, StringComparison.Ordinal) || name.Contains(VersionMarker, StringComparison.OrdinalIgnoreCase);
}
