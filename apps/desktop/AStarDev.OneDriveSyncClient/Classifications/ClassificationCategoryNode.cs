namespace AStarDev.OneDriveSyncClient.Classifications;

internal sealed record ClassificationCategoryNode
{
    public int Id { get; init; }
    public int Level { get; init; }
    public int? ParentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool? IsFamous { get; init; }
    public bool? IsInternet { get; init; }
    public bool IncludeInSearch { get; init; }
    public List<ClassificationCategoryNode> Children { get; set; } = [];
    public List<ClassificationKeywordNode> Keywords { get; set; } = [];
}
