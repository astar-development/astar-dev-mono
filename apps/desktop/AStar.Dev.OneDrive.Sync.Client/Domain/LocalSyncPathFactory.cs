using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Factory for constructing validated <see cref="LocalSyncPath"/> instances.</summary>
public static class LocalSyncPathFactory
{
    private const string InvalidPathError = "Local sync path must not be null, empty, or whitespace.";

    /// <summary>
    /// Creates a <see cref="LocalSyncPath"/> from the given raw path string.
    /// Returns a failure result when <paramref name="rawPath"/> is null, empty, or whitespace-only.
    /// </summary>
    public static Result<LocalSyncPath, ErrorResponse> Create(string? rawPath)
    {
        if(string.IsNullOrWhiteSpace(rawPath))
            return new Result<LocalSyncPath, ErrorResponse>.Error(new ErrorResponse(InvalidPathError));

        return new Result<LocalSyncPath, ErrorResponse>.Ok(LocalSyncPath.Restore(rawPath));
    }
}
