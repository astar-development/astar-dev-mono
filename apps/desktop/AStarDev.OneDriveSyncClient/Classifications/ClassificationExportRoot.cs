namespace AStarDev.OneDriveSyncClient.Classifications;

internal sealed record ClassificationExportRoot
{
    public int Version { get; init; }
    public List<ClassificationCategoryNode> Categories { get; init; } = [];
}
