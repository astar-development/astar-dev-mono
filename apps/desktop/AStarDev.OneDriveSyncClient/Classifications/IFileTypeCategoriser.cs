using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <summary>Derives a <see cref="FileClassification"/> from the file type of a remote path's extension.</summary>
public interface IFileTypeCategoriser
{
    /// <summary>Returns the type classification for <paramref name="remotePath"/>. Files whose extension is missing or unrecognised, and database and executable files, are classified as Other.</summary>
    Option<FileClassification> Categorise(string remotePath);
}
