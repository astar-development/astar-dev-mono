namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

internal sealed record ClassificationExportRoot
{
    public int Version { get; init; }
    public List<ClassificationCategoryNode> Categories { get; init; } = [];
}
