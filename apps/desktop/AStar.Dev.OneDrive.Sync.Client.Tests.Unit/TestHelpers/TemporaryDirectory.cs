using AStar.Dev.Utilities;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.TestHelpers;

internal sealed class TemporaryDirectory : IDisposable
{
    private static readonly RealFileSystem FileSystem = new();

    public string Path { get; } = FileSystem.Directory.CreateTempSubdirectory("settings-test-").FullName;
    public string SettingsFilePath => Path.CombinePath("settings.json");

    public void Dispose()
    {
        if (FileSystem.Directory.Exists(Path))
            FileSystem.Directory.Delete(Path, recursive: true);
    }
}
