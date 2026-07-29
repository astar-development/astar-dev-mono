using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;
using AStar.Dev.Infrastructure.AppDb.Entities;

namespace AStar.Dev.OneDrive.Sync.Client.Data.Repositories;

/// <summary>
///     Maps a <see cref="FileClassificationCategoryEntity"/> to a <see cref="FileClassificationCategory"/>.
/// </summary>
public static class FileClassificationCategoryToDomain
{
    extension(FileClassificationCategoryEntity fileClassificationCategoryEntity)
    {
        /// <summary>
        ///     Maps a <see cref="FileClassificationCategoryEntity"/> to a <see cref="FileClassificationCategory"/>.
        /// </summary>
        /// <returns>
        ///     A <see cref="Result{T, TError}"/> containing the mapped <see cref="FileClassificationCategory"/> or an error message.
        /// </returns>
        public Result<FileClassificationCategory, string> ToDto()
            => FileClassificationCategoryFactory.Create(
                    new FileClassificationCategoryId(fileClassificationCategoryEntity.Id),
                    fileClassificationCategoryEntity.Name,
                    fileClassificationCategoryEntity.Level,
                    fileClassificationCategoryEntity.IsFamous,
                    fileClassificationCategoryEntity.IsInternet,
                    fileClassificationCategoryEntity.ParentId.HasValue ? Option.Some(new FileClassificationCategoryId(fileClassificationCategoryEntity.ParentId.Value)) : Option.None<FileClassificationCategoryId>(),
                    fileClassificationCategoryEntity.IncludeInSearch
                );
    }
}
