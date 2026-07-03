using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.AppDb;

/// <inheritdoc />
public sealed class FileDetailResolver(IDbContextFactory<AppDbContext> contextFactory) : IFileDetailResolver
{
    /// <inheritdoc />
    public async Task<FileDetailEntity> FindOrCreateAsync(string fullPath, long? fileSizeBytes, CancellationToken cancellationToken)
    {
        int separatorIndex = fullPath.LastIndexOfAny(['/', '\\']);
        string directoryName = separatorIndex < 0 ? string.Empty : fullPath[..separatorIndex];
        string fileName = fullPath[(separatorIndex + 1)..];

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.Files.FirstOrDefaultAsync(f => f.DirectoryName.Value == directoryName && f.FileName.Value == fileName, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
            return existing;

        var created = new FileDetailEntity
        {
            FileName = new FileName(fileName),
            DirectoryName = new DirectoryName(directoryName),
            FileHandle = FileHandleFactory.Create(fullPath),
            FileSize = fileSizeBytes ?? 0
        };
        context.Files.Add(created);
        _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return created;
    }
}
