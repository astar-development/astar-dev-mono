using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.FileDetail;

/// <summary>
/// Eagerly loads a <see cref="FileEntity"/> aggregate together with its required sub-entities
/// (<see cref="FileEntity.FileAccessDetail"/>, <see cref="FileEntity.ImageDetail"/>, and <see cref="FileEntity.DeletionStatus"/>).
/// </summary>
public sealed class FileQuery : IQuery<FileEntity>
{
    /// <inheritdoc/>
    public IQueryable<FileEntity> Apply(IQueryable<FileEntity> query) =>
        query
            .Include(file => file.FileAccessDetail)
            .Include(file => file.ImageDetail)
            .Include(file => file.DeletionStatus);
}
