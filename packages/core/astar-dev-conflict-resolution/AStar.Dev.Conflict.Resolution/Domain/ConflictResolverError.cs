namespace AStar.Dev.Conflict.Resolution.Domain;

/// <summary>Base discriminated union for conflict resolver failures.</summary>
public abstract record ConflictResolverError(string Message);

/// <summary>Returned when the file operation underlying a resolution fails.</summary>
public sealed record FileOperationFailedError(string FilePath, string Reason) : ConflictResolverError($"File operation failed on {FilePath}: {Reason}");

/// <summary>Factory for <see cref="ConflictResolverError"/> subtypes.</summary>
public static class ConflictResolverErrorFactory
{
    /// <summary>Creates a <see cref="FileOperationFailedError"/>.</summary>
    public static FileOperationFailedError FileOperationFailed(string filePath, string reason) => new(filePath, reason);
}
