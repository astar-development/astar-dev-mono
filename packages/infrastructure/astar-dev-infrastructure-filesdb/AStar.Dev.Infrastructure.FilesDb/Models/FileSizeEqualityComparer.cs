namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     The <see href="FileSizeEqualityComparer"></see> class that defines how the file sizes are deemed to be equal
/// </summary>
public sealed class FileSizeEqualityComparer : IEqualityComparer<FileSize>
{
    /// <summary>
    ///     The Equals method has been overridden to perform the equality check currently required. The equality check is for
    ///     Height, Width and Length - making this more of an ImageComparer...
    /// </summary>
    /// <param name="leftFileSize">
    ///     An instance of the <see cref="FileSize" /> class to compare
    /// </param>
    /// <param name="rightFileSize">
    ///     The other instance of the <see cref="FileSize" /> class to compare
    /// </param>
    /// <returns>
    ///     <c>true</c> if the files are deemed to be the same size, <c>false</c> otherwise
    /// </returns>
    public bool Equals(FileSize? leftFileSize, FileSize? rightFileSize)
        => (rightFileSize == null && leftFileSize == null)
           || CompareFileSizes(leftFileSize, rightFileSize);

    /// <summary>
    ///     The GetHashCode has been overridden to return the hash-codes as per the fields compared in the overridden Equals
    ///     method
    /// </summary>
    /// <param name="fileSize">
    ///     The <see cref="FileSize" /> to calculate the appropriate hash-code for
    /// </param>
    /// <returns>
    ///     The hash-code, combined from the relevant properties own hash-codes
    /// </returns>
    public int GetHashCode(FileSize fileSize)
        => HashCode.Combine(fileSize.Height, fileSize.Width, fileSize.FileLength);

    private static bool CompareFileSizes(FileSize? leftFileSize, FileSize? rightFileSize)
        => leftFileSize               != null
           && rightFileSize           != null
           && leftFileSize.Height     == rightFileSize.Height
           && leftFileSize.FileLength == rightFileSize.FileLength
           && leftFileSize.Width      == rightFileSize.Width;
}