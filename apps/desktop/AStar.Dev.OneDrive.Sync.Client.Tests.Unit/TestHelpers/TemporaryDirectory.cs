using AStarDev.Utilities;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

internal sealed class TemporaryDirectory : IDisposable
{
    private static readonly MockFileSystem FileSystem = new();
    private bool disposed;

    public string Path { get; } = FileSystem.Directory.CreateTempSubdirectory("settings-test-").FullName;
    public string SettingsFilePath => Path.CombinePath("settings.json");

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed)
            return;

        disposed = true;

        if (!disposing)
            return;

        if (FileSystem.Directory.Exists(Path))
            FileSystem.Directory.Delete(Path, recursive: true);
    }
}
