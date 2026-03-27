using System;
using System.IO;
using System.Linq;

namespace AStar.Dev.Utilities;

/// <summary>
///     Provides extension methods for common path operations.
/// </summary> <remarks>
///     This class includes methods that enhance the functionality of string paths, such as safely combining multiple segments while preventing rooted paths from overriding the base path.
/// </remarks>
public static class PathOperationExtensions
{
    /// <summary>
    ///     Combines a base path with one or more relative segments while preventing rooted segments from overriding earlier parts.
    /// </summary>
    /// <param name="basePath">The starting path to append segments to.</param>
    /// <param name="segments">The path segments to append. All segments must be relative.</param>
    /// <returns>The combined path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="basePath" /> or <paramref name="segments" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when any segment is rooted.</exception>
    public static string CombinePath(this string basePath, params string[] segments)
    {
        var combined = basePath;

        foreach(var segment in segments.Where(s => s is not null))
        {
            if(Path.IsPathRooted(segment))
            {
                throw new ArgumentException("Path segments must be relative.", nameof(segments));
            }

            combined = Path.Join(combined, segment);
        }

        return combined;
    }
}
