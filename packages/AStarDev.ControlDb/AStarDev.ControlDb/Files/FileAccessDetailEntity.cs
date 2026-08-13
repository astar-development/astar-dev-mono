namespace AStarDev.ControlDb.Files;

/// <summary>Tracks when a file's details were last refreshed and when it was last viewed.</summary>
public sealed class FileAccessDetailEntity
{
    /// <summary>Primary key.</summary>
    public FileAccessDetailId Id { get; set; }

    /// <summary>Foreign key to the parent <see cref="FileEntity"/>.</summary>
    public FileId FileEntityId { get; set; }

    /// <summary>Navigation property to the parent file entity.</summary>
    public FileEntity FileDetail { get; set; } = null!;

    /// <summary>The date the file's details were last updated, or null if never updated.</summary>
    public DateTimeOffset? DetailsLastUpdated { get; set; }

    /// <summary>The date the file was last viewed, or null if never viewed.</summary>
    public DateTimeOffset? LastViewed { get; set; }

    /// <summary>Whether the file has been marked as needing to move.</summary>
    public bool MoveRequired { get; set; }
}
