namespace AStar.Dev.OneDrive.Client.Features.FileOperations;

/// <summary>Represents the outcome of a successful file upload.</summary>
public sealed record FileUploadResult(string RemoteItemId, string FileName, long BytesUploaded);

/// <summary>Factory for <see cref="FileUploadResult"/>.</summary>
public static class FileUploadResultFactory
{
    /// <summary>Creates a <see cref="FileUploadResult"/>.</summary>
    public static FileUploadResult Create(string remoteItemId, string fileName, long bytesUploaded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteItemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return new FileUploadResult(remoteItemId, fileName, bytesUploaded);
    }
}
