using AStarDev.CloudSync.Core.Common;
using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.ViewModels.Common;

/// <summary>Shortens local paths for display by showing the user's home folder as <c>~</c>.</summary>
/// <param name="homeFolder">Supplies the user's home folder.</param>
public sealed class LocalPathFormatter(IHomeFolder homeFolder)
{
    private const string HomeSymbol = "~";

    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    private static StringComparison PathComparison => OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Returns <paramref name="path"/> with a leading home folder replaced by <c>~</c>, e.g. "~/CloudSync/OneDrive". Paths outside the home folder, blank paths and paths when there is no home folder are returned unchanged. Never throws.</summary>
    /// <param name="path">The full local path.</param>
    /// <returns>The shortened path, or <paramref name="path"/> unchanged.</returns>
    public string Format(string path) => homeFolder.Folder.Match(home => Shorten(path ?? string.Empty, home.FullName.TrimEnd(Separators)), () => path ?? string.Empty);

    private static string Shorten(string path, string home)
    {
        if (home.Length == 0 || !path.StartsWith(home, PathComparison))
            return path;

        if (path.Length == home.Length)
            return HomeSymbol;

        return Separators.Contains(path[home.Length]) ? HomeSymbol + path[home.Length..] : path;
    }
}
