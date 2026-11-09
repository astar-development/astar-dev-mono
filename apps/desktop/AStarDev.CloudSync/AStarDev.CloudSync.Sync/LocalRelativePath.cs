using AStarDev.SourceGeneratorAttributes;

namespace AStarDev.CloudSync.Sync;

/// <summary>A folder path relative to an account's sync root, with <c>/</c> separators and composed (NFC) Unicode. The root itself is the empty path. It never holds an absolute path or a <c>..</c> segment, so a caller can never list outside the root. Two paths are equal when they differ only in case.</summary>
[StrongType(typeof(string))]
public partial record struct LocalRelativePath
{
    /// <summary>Compares the paths ignoring case.</summary>
    /// <param name="other">The path to compare with.</param>
    public readonly bool Equals(LocalRelativePath other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <summary>A hash code that ignores case.</summary>
    public override readonly int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

    /// <summary>Returns a redacted form, so the path never reaches logs by accident.</summary>
    public override readonly string ToString() => "LocalRelativePath(redacted)";
}
