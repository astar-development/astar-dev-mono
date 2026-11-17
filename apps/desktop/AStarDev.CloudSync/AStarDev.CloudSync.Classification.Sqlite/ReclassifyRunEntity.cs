namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class ReclassifyRunEntity
{
    public const int SingleRowId = 1;

    public int Id { get; set; } = SingleRowId;

    public string EngineVersion { get; set; } = string.Empty;

    public string? SeedVersion { get; set; }

    public DateTimeOffset FinishedAt { get; set; }
}
