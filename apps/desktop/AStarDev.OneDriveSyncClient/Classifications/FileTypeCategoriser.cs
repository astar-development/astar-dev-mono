using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class FileTypeCategoriser(IFileTypeClassifier fileTypeClassifier) : IFileTypeCategoriser
{
    /// <inheritdoc />
    public Option<FileClassification> Categorise(string remotePath)
    {
        _ = fileTypeClassifier;

        return Option.None<FileClassification>();
    }
}
