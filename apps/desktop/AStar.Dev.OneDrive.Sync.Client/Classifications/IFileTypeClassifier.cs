namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Classifies a file extension into a broad <see cref="FileType"/> category.</summary>
public interface IFileTypeClassifier
{
    /// <summary>Returns the <see cref="FileType"/> for the given file extension.</summary>
    /// <param name="fileExtension">The file extension including the leading dot, e.g. <c>.jpg</c>. Null or empty returns <see cref="FileType.Unknown"/>.</param>
    FileType Classify(string fileExtension);
}
