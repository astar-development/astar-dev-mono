namespace AStar.Dev.OneDrive.Sync.Client.Domain;

/// <summary>Derives a <see cref="FileClassification"/> from a full remote file path.</summary>
public interface IFileAutoCategorisor
{
    /// <summary>Derives a <see cref="FileClassification"/> from a full remote file path.</summary>
    FileClassification Categorise(string remotePath);
}
