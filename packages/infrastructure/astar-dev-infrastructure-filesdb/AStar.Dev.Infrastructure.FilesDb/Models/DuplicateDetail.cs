using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     The <see cref="DuplicatesDetails" /> class defines the fields that will be mapped from the vw_DuplicatesDetails in the database
/// </summary>
[Keyless]
public class DuplicatesDetails
{
    /// <summary>
    ///     The ID of the <see cref="FileDetail" /> in the Duplicates list
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The FileAccessDetailId of the joined <see cref="FileAccessDetail" /> table. The joined data is not in the Duplicates list
    /// </summary>
    public int FileAccessDetailId { get; set; }

    /// <summary>
    ///     Gets or sets the File Name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the Directory Name
    /// </summary>
    public string DirectoryName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the File Height
    /// </summary>
    public int Height { get; set; }

  /// <summary>
    ///     Gets or sets the File Width
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    ///     Gets or sets the File Size
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    ///     Gets or sets the File Handle
    /// </summary>
    public string FileHandle { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets whether File is an image
    /// </summary>
    public bool IsImage { get; set; }

    /// <summary>
    ///     Gets or sets the Instance count for the duplicate group
    /// </summary>
    public int Instances { get; set; }

    /// <summary>
    ///     Gets or sets the Details Last Updated
    /// </summary>
    public DateTime DetailsLastUpdated { get; set; }

    /// <summary>
    ///     Gets or sets the Last Viewed date
    /// </summary>
    public DateTime? LastViewed { get; set; }

    /// <summary>
    ///     Gets or sets the Soft Deleted flag
    /// </summary>
    public bool SoftDeleted { get; set; }

    /// <summary>
    ///     Gets or sets the SoftDeletePending flag
    /// </summary>
    public bool SoftDeletePending { get; set; }

    /// <summary>
    ///     Gets or sets the Move Required flag
    /// </summary>
    public bool MoveRequired { get; set; }

    /// <summary>
    ///     Gets or sets the Hard Delete Pending flag
    /// </summary>
    public bool HardDeletePending { get; set; }
}