namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>Base discriminated union for conflict detection failures.</summary>
public abstract record ConflictDetectionError(string Message);

/// <summary>Returned when access to the local file system is denied.</summary>
public sealed record LocalFileAccessDeniedError(string FilePath) : ConflictDetectionError($"Access denied to local file: {FilePath}");

/// <summary>Factory for <see cref="ConflictDetectionError"/> subtypes.</summary>
public static class ConflictDetectionErrorFactory
{
    /// <summary>Creates a <see cref="LocalFileAccessDeniedError"/>.</summary>
    public static LocalFileAccessDeniedError AccessDenied(string filePath) => new(filePath);
}
