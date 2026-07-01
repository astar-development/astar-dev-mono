namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     Records the classification applied to a file at download time.
/// </summary>
public class DownloadedFileClassification : AuditableEntity
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the id of the classified file.</summary>
    public FileId FileDetailId { get; set; }

    /// <summary>Gets or sets the classified file.</summary>
    public FileDetail FileDetail { get; set; } = null!;

    /// <summary>Gets or sets the id of the applied classification.</summary>
    public int FileClassificationId { get; set; }

    /// <summary>Gets or sets the applied classification.</summary>
    public FileClassification FileClassification { get; set; } = null!;
}
