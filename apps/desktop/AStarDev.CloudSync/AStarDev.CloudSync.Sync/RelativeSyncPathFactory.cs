using AStarDev.FunctionalParadigm;

namespace AStarDev.CloudSync.Sync;

/// <summary>Creates <see cref="RelativeSyncPath"/> values.</summary>
public static class RelativeSyncPathFactory
{
    /// <summary>Creates a path. Backslashes become <c>/</c> and leading or trailing separators are removed.</summary>
    /// <param name="value">The relative path.</param>
    /// <returns>The path, or an error when it is empty or contains a <c>.</c> or <c>..</c> segment.</returns>
    public static Result<RelativeSyncPath, ErrorResponse> Create(string? value) => throw new NotImplementedException($"{value}");
}
