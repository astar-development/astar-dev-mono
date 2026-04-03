namespace AStar.Dev.OneDrive.Client.Features.FileOperations;

/// <summary>Represents the outcome of a successful file download.</summary>
public sealed record FileDownloadResult(string LocalPath, long BytesWritten);

/// <summary>Factory for <see cref="FileDownloadResult"/>.</summary>
public static class FileDownloadResultFactory
{
    /// <summary>Creates a <see cref="FileDownloadResult"/>.</summary>
    public static FileDownloadResult Create(string localPath, long bytesWritten)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localPath);

        return new FileDownloadResult(localPath, bytesWritten);
    }
}
