namespace AStarDev.CloudSync.Classification.Sqlite;

internal sealed class TermEntity
{
    public Guid Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string Source { get; set; } = string.Empty;

    public bool IsIgnored { get; set; }
}
