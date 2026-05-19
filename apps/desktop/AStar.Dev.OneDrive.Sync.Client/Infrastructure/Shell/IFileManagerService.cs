namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Shell;

public interface IFileManagerService
{
    /// <summary>Opens the specified folder in the platform's native file manager.</summary>
    /// <param name="path">The absolute path of the folder to open.</param>
    void OpenFolder(string path);
}
