using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.Infrastructure.AppDb;

/// <summary>Resolves the canonical <see cref="FileDetailEntity"/> for a physical file path, creating one when none exists, so every application shares one file identity per path.</summary>
public interface IFileDetailResolver
{
    /// <summary>Finds the <see cref="FileDetailEntity"/> whose directory and file name match <paramref name="fullPath"/>, creating and persisting one when no match exists.</summary>
    Task<FileDetailEntity> FindOrCreateAsync(string fullPath, long? fileSizeBytes, CancellationToken cancellationToken);
}
