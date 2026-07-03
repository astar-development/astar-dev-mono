namespace AStar.Dev.Infrastructure.AppDb.Entities;

/// <summary>Tracks when a file's details were last refreshed and when it was last viewed.</summary>
public sealed class FileAccessDetailEntity
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The date the file's details were last updated, or null if never updated.</summary>
    public DateTimeOffset? DetailsLastUpdated { get; set; }

    /// <summary>The date the file was last viewed, or null if never viewed.</summary>
    public DateTimeOffset? LastViewed { get; set; }

    /// <summary>Whether the file has been marked as needing to move.</summary>
    public bool MoveRequired { get; set; }
}
