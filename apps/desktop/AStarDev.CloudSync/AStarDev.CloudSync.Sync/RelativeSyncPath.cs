using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>A file or folder path relative to an account's sync root, with <c>/</c> separators and composed (NFC) Unicode, used on both the local and the remote side. Two paths are equal when they differ only in case, as on OneDrive.</summary>
[StrongType(typeof(string))]
public partial record struct RelativeSyncPath
{
    /// <summary>Compares the paths ignoring case.</summary>
    /// <param name="other">The path to compare with.</param>
    public readonly bool Equals(RelativeSyncPath other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <summary>A hash code that ignores case.</summary>
    public override readonly int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

    /// <summary>Returns a redacted form, so the path never reaches logs by accident.</summary>
    public override readonly string ToString() => "RelativeSyncPath(redacted)";
}
