using System.Diagnostics.CodeAnalysis;

namespace AStarDev.CloudSync.Sync;

/// <summary>The host's default file system case rule: Windows and macOS file systems ignore case by default, Linux file systems do not.</summary>
[ExcludeFromCodeCoverage(Justification = "Reads the host operating system only; the stores that use the rule are tested with a fixed rule.")]
public sealed class SystemFileSystemCaseRule : IFileSystemCaseRule
{
    /// <inheritdoc />
    public StringComparison Comparison => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
