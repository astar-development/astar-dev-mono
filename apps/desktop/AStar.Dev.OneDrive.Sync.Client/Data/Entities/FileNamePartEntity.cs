namespace AStar.Dev.OneDrive.Sync.Client.Data.Entities;

/// <summary>A segment of a file name, used for search and classification.</summary>
public sealed class FileNamePartEntity : AuditableEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The text content of the file name part.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Whether files associated with this file name part should be included in search results.</summary>
    public bool IncludeInSearch { get; set; }
}
