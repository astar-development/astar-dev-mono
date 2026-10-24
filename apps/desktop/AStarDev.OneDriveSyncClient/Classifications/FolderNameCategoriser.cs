using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class FolderNameCategoriser : IFolderNameCategoriser
{
    /// <inheritdoc />
    public IReadOnlyList<FileClassification> Categorise(string remotePath) => [];
}
