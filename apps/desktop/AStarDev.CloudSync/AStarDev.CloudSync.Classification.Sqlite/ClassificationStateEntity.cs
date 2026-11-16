namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class ClassificationStateEntity
{
    public const int SingleRowId = 1;

    public int Id { get; set; } = SingleRowId;

    public string SeedVersion { get; set; } = string.Empty;
}
