namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IFileOpenerService
{
    /// <summary>Opens the specified file in the platform's default application. No-op if the file does not exist.</summary>
    /// <param name="localPath">The absolute local path of the file to open.</param>
    void OpenFile(string localPath);
}
